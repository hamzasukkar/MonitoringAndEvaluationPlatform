/*!
 * searchable-select.js
 * Turns every native <select> on the page into a searchable dropdown.
 *
 * - No dependencies (jQuery is optional and only used to catch $().trigger('change')).
 * - The original <select> stays in the DOM, so model binding, asp-for, validation,
 *   this.form.submit(), $('#x').val(...) and getElementById('x').value keep working.
 * - Selects added to the page later (modals, AJAX, inline editors) are picked up
 *   automatically.
 *
 * Opt out of a single dropdown with data-searchable="false" (or class "no-searchable"),
 * or of a whole region by putting data-searchable="false" on any ancestor.
 */
(function () {
    'use strict';

    if (window.SearchableSelect) return;

    var CFG = {
        // Show the search box only when the list has at least this many options (0 = always).
        minOptionsForSearch: 0,
        // Hard cap on rendered rows, so a select with thousands of options stays fast.
        maxRendered: 400,
        minPanelWidth: 240,
        maxPanelHeight: 320,
        // Never touch these: select2 brings its own search UI, and these opt-outs are explicit.
        skip: '[data-searchable="false"],.no-searchable,.select2-hidden-accessible,[data-select2-id],[data-ss-skip]'
    };

    var STR = {
        en: { search: 'Search...', empty: 'No matches found', placeholder: 'Select...', selected: '{0} selected', clear: 'Clear', more: '{0} more - keep typing to narrow' },
        ar: { search: 'بحث...', empty: 'لا توجد نتائج', placeholder: 'اختر...', selected: '{0} عنصر محدد', clear: 'مسح', more: 'و {0} غيرها - تابع الكتابة للتصفية' },
        fr: { search: 'Rechercher...', empty: 'Aucun résultat', placeholder: 'Sélectionner...', selected: '{0} sélectionné(s)', clear: 'Effacer', more: '{0} de plus - continuez à taper' }
    };
    var L = STR[(document.documentElement.lang || 'en').slice(0, 2).toLowerCase()] || STR.en;

    var uid = 0;
    var openWidget = null;
    var ICON_SEARCH = '<svg viewBox="0 0 16 16" width="14" height="14" aria-hidden="true" focusable="false"><path fill="currentColor" d="M11.74 10.34h-.79l-.28-.27a6.47 6.47 0 1 0-.7.7l.27.28v.79l5 4.99L16.99 15l-4.99-5zm-6 0a4.5 4.5 0 1 1 0-9 4.5 4.5 0 0 1 0 9z"/></svg>';
    var ICON_CHECK = '<svg viewBox="0 0 16 16" width="14" height="14" aria-hidden="true" focusable="false"><path fill="currentColor" d="M13.5 3.5 6 11 2.5 7.5 1 9l5 5 9-9z"/></svg>';

    /* ---------------------------------------------------------------- helpers */

    function fmt(s, v) { return s.replace('{0}', v); }

    function esc(s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    // Case/accent/tashkeel-insensitive, so Arabic and French search the way users type.
    function normalize(s) {
        s = (s == null ? '' : String(s)).toLowerCase();
        try { s = s.normalize('NFD').replace(/[̀-ͯ]/g, ''); } catch (e) { /* older browsers */ }
        return s
            .replace(/[ً-ٟـٰ]/g, '')
            .replace(/[أإآٱ]/g, 'ا')
            .replace(/ى/g, 'ي')
            .replace(/ة/g, 'ه')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function isHiddenSelect(select) {
        if (select.hasAttribute('hidden') || select.classList.contains('d-none')) return true;
        var st = select.getAttribute('style') || '';
        return /display\s*:\s*none/i.test(st) || /visibility\s*:\s*hidden/i.test(st);
    }

    function shouldSkip(select) {
        if (select.matches(CFG.skip)) return true;
        if (select.closest('[data-searchable="false"]')) return true;
        if (select.closest('.ss-panel')) return true;
        return false;
    }

    /* ----------------------------------------------------------------- widget */

    function Widget(select) {
        var self = this;
        this.select = select;
        this.id = 'ss-' + (++uid);
        this.multiple = select.multiple;
        this.panel = null;
        this.items = [];       // rendered row -> option index
        this.activeIndex = -1;

        var inline = this.isInlineContext();
        var nativeWidth = select.getBoundingClientRect().width;

        var wrap = document.createElement('div');
        wrap.className = 'ss' + (inline ? ' ss--inline' : '');

        var trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.className = (select.className ? select.className + ' ' : '') + 'ss-control' + (isBare(select) ? ' ss-control--bare' : '');
        if (select.getAttribute('style')) trigger.setAttribute('style', select.getAttribute('style'));
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-expanded', 'false');
        trigger.innerHTML = '<span class="ss-label"></span><span class="ss-caret" aria-hidden="true"></span>';

        var label = select.id ? document.querySelector('label[for="' + cssEscape(select.id) + '"]') : null;
        if (label) {
            if (!label.id) label.id = this.id + '-label';
            trigger.setAttribute('aria-labelledby', label.id);
            label.addEventListener('click', function (e) { e.preventDefault(); self.toggle(); });
        } else if (select.getAttribute('aria-label')) {
            trigger.setAttribute('aria-label', select.getAttribute('aria-label'));
        } else if (select.getAttribute('title')) {
            trigger.setAttribute('title', select.getAttribute('title'));
        }

        select.parentNode.insertBefore(wrap, select);
        wrap.appendChild(select);
        wrap.appendChild(trigger);
        select.classList.add('ss-native');
        select.tabIndex = -1;

        this.wrap = wrap;
        this.trigger = trigger;
        this.labelEl = trigger.querySelector('.ss-label');

        // A shrink-to-fit wrapper (inline flow, or an item in a flex/grid row) gives a
        // percentage width on the control nothing to resolve against, so the control can
        // come out narrower than the select it replaced. Pin the wrapper to the width the
        // select was actually using; it can still shrink with its container.
        if (nativeWidth > 0 && trigger.getBoundingClientRect().width < nativeWidth - 2) {
            wrap.style.width = nativeWidth + 'px';
        }

        trigger.addEventListener('click', function (e) { e.preventDefault(); e.stopPropagation(); self.toggle(); });
        trigger.addEventListener('keydown', function (e) { self.onTriggerKey(e); });
        // Anything that focuses the hidden select (jQuery validation, custom code)
        // should land on the visible control instead.
        select.addEventListener('focus', function () { if (openWidget !== self) trigger.focus(); });
        select.addEventListener('change', function () { self.sync(); });
        if (select.form) select.form.addEventListener('reset', function () { setTimeout(function () { self.sync(); }, 0); });

        // Options rebuilt by cascading filters, enabled/disabled, or hidden with d-none.
        this.observer = new MutationObserver(function () {
            self.sync();
            if (openWidget === self) self.renderList();
        });
        this.observer.observe(select, {
            childList: true, subtree: true, attributes: true,
            attributeFilter: ['disabled', 'class', 'style', 'hidden', 'multiple']
        });

        select.__ss = this;
        select.setAttribute('data-ss-enhanced', 'true');
        this.sync();
    }

    function cssEscape(v) {
        return (window.CSS && CSS.escape) ? CSS.escape(v) : String(v).replace(/(["\\\]\[])/g, '\\$1');
    }

    // Every native select computes to inline-block, so display alone says nothing. What
    // matters is whether the select shares its line with other content: if it does, the
    // wrapper must stay inline, otherwise a block wrapper gives percentage widths
    // (form-select's 100%, the language picker's calc(100% - 2rem)) a container to
    // resolve against.
    Widget.prototype.isInlineContext = function () {
        var select = this.select;
        var parent = select.parentElement;
        if (!parent) return false;

        var pd = window.getComputedStyle(parent).display;
        if (pd.indexOf('flex') > -1 || pd.indexOf('grid') > -1) return false; // items are blockified anyway

        for (var n = parent.firstChild; n; n = n.nextSibling) {
            if (n === select) continue;
            if (n.nodeType === 3 && n.nodeValue.trim() !== '') return true;
            if (n.nodeType === 1 && window.getComputedStyle(n).display.indexOf('inline') === 0) return true;
        }
        return false;
    };

    // A select with no styling classes of its own has nothing for the control to inherit,
    // so it gets our minimal baseline look.
    function isBare(select) {
        return !/form-select|form-control|select|picker|input|filter|dropdown|btn/i.test(select.className || '');
    }

    Widget.prototype.selectedOptions = function () {
        var out = [], opts = this.select.options;
        for (var i = 0; i < opts.length; i++) if (opts[i].selected) out.push(opts[i]);
        return out;
    };

    Widget.prototype.placeholderText = function () {
        var first = this.select.options[0];
        if (first && first.value === '' && (first.text || '').trim() !== '') return first.text.trim();
        return this.select.getAttribute('data-placeholder') || L.placeholder;
    };

    // Mirror the native select's state onto the visible control.
    Widget.prototype.sync = function () {
        var texts = this.selectedOptions()
            .map(function (o) { return (o.text || '').trim(); })
            .filter(function (t) { return t !== ''; });
        var placeholder = false;

        if (!texts.length) {
            placeholder = true;
            this.labelEl.textContent = this.placeholderText();
        } else if (this.multiple && texts.length > 2) {
            this.labelEl.textContent = fmt(L.selected, texts.length);
        } else {
            this.labelEl.textContent = texts.join(', ');
        }
        this.labelEl.classList.toggle('ss-label--placeholder', placeholder);
        this.trigger.title = placeholder ? '' : texts.join(', ');

        this.trigger.disabled = this.select.disabled;
        this.wrap.classList.toggle('ss--disabled', this.select.disabled);
        this.wrap.classList.toggle('ss--hidden', isHiddenSelect(this.select));
        if (this.select.disabled && openWidget === this) this.close(false);
    };

    /* -------------------------------------------------------------- open/close */

    Widget.prototype.toggle = function () {
        if (openWidget === this) this.close(); else this.open();
    };

    Widget.prototype.open = function () {
        if (this.select.disabled) return;
        if (openWidget && openWidget !== this) openWidget.close(false);

        var self = this;
        var panel = document.createElement('div');
        panel.className = 'ss-panel';
        panel.id = this.id + '-panel';
        panel.setAttribute('dir', window.getComputedStyle(this.select).direction || 'ltr');

        var showSearch = this.select.options.length >= CFG.minOptionsForSearch;
        panel.innerHTML =
            (showSearch
                ? '<div class="ss-search"><span class="ss-search-icon">' + ICON_SEARCH + '</span>' +
                  '<input type="text" class="ss-search-input" autocomplete="off" spellcheck="false" placeholder="' + esc(L.search) + '"></div>'
                : '') +
            '<ul class="ss-list" role="listbox"' + (this.multiple ? ' aria-multiselectable="true"' : '') + '></ul>' +
            (this.multiple
                ? '<div class="ss-foot"><span class="ss-count"></span><button type="button" class="ss-clear">' + esc(L.clear) + '</button></div>'
                : '');

        // Bootstrap modals and SweetAlert popups pull focus back to themselves, so the
        // panel has to live inside them; everywhere else <body> is the safest host.
        (this.select.closest('.modal, .swal2-container, [data-ss-portal]') || document.body).appendChild(panel);
        this.panel = panel;
        this.list = panel.querySelector('.ss-list');
        this.search = panel.querySelector('.ss-search-input');

        this.wrap.classList.add('ss--open');
        this.trigger.setAttribute('aria-expanded', 'true');
        this.trigger.setAttribute('aria-controls', panel.id);
        openWidget = this;

        this.renderList();
        this.position();

        if (this.search) {
            this.search.addEventListener('input', function () { self.renderList(); self.position(); });
            this.search.addEventListener('keydown', function (e) { self.onPanelKey(e); });
            this.search.focus();
        } else {
            panel.tabIndex = -1;
            panel.addEventListener('keydown', function (e) { self.onPanelKey(e); });
            panel.focus();
        }

        // Keep focus in the search box while clicking rows.
        this.list.addEventListener('mousedown', function (e) { e.preventDefault(); });
        this.list.addEventListener('click', function (e) {
            var row = e.target.closest('.ss-option');
            if (!row || row.classList.contains('ss-option--disabled')) return;
            self.choose(parseInt(row.getAttribute('data-index'), 10));
        });

        var clear = panel.querySelector('.ss-clear');
        if (clear) clear.addEventListener('click', function () {
            var opts = self.select.options;
            for (var i = 0; i < opts.length; i++) opts[i].selected = false;
            self.emitChange();
            self.renderList();
            if (self.search) self.search.focus();
        });
    };

    Widget.prototype.close = function (focusTrigger) {
        if (openWidget !== this) return;
        openWidget = null;
        if (this.panel && this.panel.parentNode) this.panel.parentNode.removeChild(this.panel);
        this.panel = this.list = this.search = null;
        this.activeIndex = -1;
        this.wrap.classList.remove('ss--open');
        this.trigger.setAttribute('aria-expanded', 'false');
        this.trigger.removeAttribute('aria-controls');
        if (focusTrigger !== false) this.trigger.focus();
    };

    // The panel lives on <body> with fixed positioning, so overflow:hidden cards,
    // table cells and modals cannot clip it.
    Widget.prototype.position = function () {
        if (!this.panel) return;
        var r = this.trigger.getBoundingClientRect();
        // The control went away (modal closed, row re-rendered): drop the panel with it.
        if (!this.trigger.isConnected || (r.width === 0 && r.height === 0)) { this.close(false); return; }
        var vw = document.documentElement.clientWidth;
        var vh = document.documentElement.clientHeight;
        var rtl = this.panel.getAttribute('dir') === 'rtl';

        var width = Math.min(Math.max(r.width, CFG.minPanelWidth), vw - 16);
        this.panel.style.width = width + 'px';

        var below = vh - r.bottom - 8;
        var above = r.top - 8;
        var chrome = (this.search ? 46 : 0) + (this.multiple ? 38 : 0) + 8;
        this.list.style.maxHeight = Math.max(120, Math.min(CFG.maxPanelHeight, Math.max(below, above) - chrome)) + 'px';

        var left = rtl ? r.right - width : r.left;
        this.panel.style.left = Math.max(8, Math.min(left, vw - width - 8)) + 'px';

        var h = this.panel.offsetHeight;
        var placeAbove = below < h && above > below;
        this.panel.style.top = (placeAbove ? Math.max(8, r.top - h - 4) : r.bottom + 4) + 'px';
        this.panel.classList.toggle('ss-panel--above', placeAbove);
    };

    /* ----------------------------------------------------------------- render */

    Widget.prototype.renderList = function () {
        if (!this.list) return;
        var q = normalize(this.search ? this.search.value : '');
        var opts = this.select.options;
        var html = [], lastGroup = null, shown = 0, hiddenCount = 0;
        this.items = [];

        for (var i = 0; i < opts.length; i++) {
            var o = opts[i];
            var text = (o.text || '').trim();
            if (q && normalize(text + ' ' + (o.value || '') + ' ' + (o.getAttribute('data-search') || '')).indexOf(q) === -1) continue;
            if (shown >= CFG.maxRendered) { hiddenCount++; continue; }

            var group = (o.parentElement && o.parentElement.tagName === 'OPTGROUP') ? o.parentElement.label : null;
            if (group && group !== lastGroup) html.push('<li class="ss-group">' + esc(group) + '</li>');
            lastGroup = group;

            var cls = 'ss-option';
            if (o.selected) cls += ' ss-option--selected';
            if (o.disabled) cls += ' ss-option--disabled';
            if (text === '') text = this.placeholderText();

            html.push(
                '<li class="' + cls + '" role="option" aria-selected="' + (o.selected ? 'true' : 'false') + '" data-index="' + i + '">' +
                (this.multiple ? '<span class="ss-box">' + ICON_CHECK + '</span>' : '') +
                '<span class="ss-option-text">' + esc(text) + '</span>' +
                (this.multiple ? '' : '<span class="ss-tick">' + ICON_CHECK + '</span>') +
                '</li>'
            );
            this.items.push(i);
            shown++;
        }

        if (!html.length) html.push('<li class="ss-empty">' + esc(L.empty) + '</li>');
        else if (hiddenCount) html.push('<li class="ss-more">' + esc(fmt(L.more, hiddenCount)) + '</li>');
        this.list.innerHTML = html.join('');

        var count = this.panel ? this.panel.querySelector('.ss-count') : null;
        if (count) count.textContent = fmt(L.selected, this.selectedOptions().length);

        // Start on the first selected row, otherwise on the first selectable one.
        var startAt = -1;
        for (var k = 0; k < this.items.length; k++) {
            if (opts[this.items[k]].selected && !opts[this.items[k]].disabled) { startAt = k; break; }
        }
        this.setActive(startAt >= 0 ? startAt : this.nextSelectable(-1, 1), true);
    };

    Widget.prototype.rows = function () {
        return this.list ? this.list.querySelectorAll('.ss-option') : [];
    };

    Widget.prototype.nextSelectable = function (from, dir) {
        var opts = this.select.options;
        for (var i = from + dir; i >= 0 && i < this.items.length; i += dir) {
            if (!opts[this.items[i]].disabled) return i;
        }
        return (from >= 0 && from < this.items.length) ? from : -1;
    };

    Widget.prototype.setActive = function (i, initial) {
        var rows = this.rows();
        if (!rows.length) { this.activeIndex = -1; return; }
        if (i < 0) i = 0;
        if (i >= rows.length) i = rows.length - 1;
        for (var k = 0; k < rows.length; k++) rows[k].classList.toggle('ss-option--active', k === i);
        this.activeIndex = i;

        var row = rows[i];
        if (!row) return;
        if (initial) {
            // Scroll the list itself, never the page, when the panel first opens.
            var lr = this.list.getBoundingClientRect(), rr = row.getBoundingClientRect();
            if (rr.top < lr.top || rr.bottom > lr.bottom) {
                this.list.scrollTop = row.offsetTop - (this.list.clientHeight - row.offsetHeight) / 2;
            }
        } else {
            row.scrollIntoView({ block: 'nearest' });
        }
    };

    /* -------------------------------------------------------------- selection */

    Widget.prototype.choose = function (optionIndex) {
        var o = this.select.options[optionIndex];
        if (!o || o.disabled) return;

        if (this.multiple) {
            o.selected = !o.selected;
            this.emitChange();
            this.renderList();
            if (this.search) this.search.focus();
        } else {
            if (this.select.selectedIndex !== optionIndex) {
                this.select.selectedIndex = optionIndex;
                this.emitChange();
            }
            this.close();
        }
    };

    // One native, bubbling change event: inline onchange="", addEventListener and
    // jQuery .on('change') handlers all see it.
    Widget.prototype.emitChange = function () {
        this.sync();
        this.select.dispatchEvent(new Event('input', { bubbles: true }));
        this.select.dispatchEvent(new Event('change', { bubbles: true }));
    };

    /* --------------------------------------------------------------- keyboard */

    Widget.prototype.onTriggerKey = function (e) {
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp' || e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            this.open();
        } else if (e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey) {
            // Start typing straight into the search box.
            this.open();
            if (this.search) { this.search.value = e.key; this.renderList(); e.preventDefault(); }
        }
    };

    Widget.prototype.onPanelKey = function (e) {
        switch (e.key) {
            case 'ArrowDown': e.preventDefault(); this.setActive(this.nextSelectable(this.activeIndex, 1)); break;
            case 'ArrowUp': e.preventDefault(); this.setActive(this.nextSelectable(this.activeIndex, -1)); break;
            case 'Home': e.preventDefault(); this.setActive(this.nextSelectable(-1, 1)); break;
            case 'End': e.preventDefault(); this.setActive(this.nextSelectable(this.items.length, -1)); break;
            case 'Enter':
                e.preventDefault();
                if (this.activeIndex >= 0 && this.items[this.activeIndex] != null) this.choose(this.items[this.activeIndex]);
                break;
            case 'Escape': e.preventDefault(); e.stopPropagation(); this.close(); break;
            case 'Tab': this.close(false); break;
        }
    };

    Widget.prototype.destroy = function () {
        this.close(false);
        if (this.observer) this.observer.disconnect();
        this.select.classList.remove('ss-native');
        this.select.removeAttribute('data-ss-enhanced');
        this.select.removeAttribute('tabindex');
        if (this.wrap.parentNode) {
            this.wrap.parentNode.insertBefore(this.select, this.wrap);
            this.wrap.parentNode.removeChild(this.wrap);
        }
        delete this.select.__ss;
    };

    /* ------------------------------------------------------------- public API */

    function enhance(select) {
        if (!select || select.tagName !== 'SELECT' || select.__ss || shouldSkip(select)) return null;
        try {
            return new Widget(select);
        } catch (err) {
            if (window.console) console.warn('searchable-select:', err);
            return null;
        }
    }

    function enhanceAll(root) {
        var scope = root || document;
        if (!scope.querySelectorAll) return;
        var list = scope.querySelectorAll('select');
        for (var i = 0; i < list.length; i++) enhance(list[i]);
    }

    function refresh(target) {
        if (target && target.__ss) { target.__ss.sync(); return; }
        var list = (target || document).querySelectorAll('select[data-ss-enhanced]');
        for (var i = 0; i < list.length; i++) if (list[i].__ss) list[i].__ss.sync();
    }

    function destroy(select) {
        if (select && select.__ss) select.__ss.destroy();
    }

    window.SearchableSelect = {
        enhance: enhance, enhanceAll: enhanceAll, refresh: refresh, destroy: destroy,
        config: CFG, version: '1.0.0'
    };

    /* -------------------------------------------------------------- wiring up */

    document.addEventListener('mousedown', function (e) {
        if (!openWidget || !e.target || !e.target.closest) return;
        if (e.target.closest('.ss-panel')) return;
        if (e.target.closest('.ss') === openWidget.wrap) return;
        openWidget.close(false);
    }, true);

    window.addEventListener('resize', function () { if (openWidget) openWidget.position(); });
    window.addEventListener('scroll', function () { if (openWidget) openWidget.position(); }, true);

    // jQuery's .trigger('change') never reaches native listeners, so mirror it here.
    var jqHooked = false;
    function hookJQuery() {
        if (jqHooked || !window.jQuery) return;
        jqHooked = true;
        window.jQuery(document).on('change.searchableSelect', 'select', function () {
            if (this.__ss) this.__ss.sync();
        });
    }

    function boot() {
        hookJQuery();
        enhanceAll(document);
        new MutationObserver(function (records) {
            for (var i = 0; i < records.length; i++) {
                var added = records[i].addedNodes;
                for (var j = 0; j < added.length; j++) {
                    var n = added[j];
                    if (n.nodeType !== 1) continue;
                    if (n.tagName === 'SELECT') enhance(n);
                    else enhanceAll(n);
                }
            }
        }).observe(document.documentElement, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
    else boot();
    window.addEventListener('load', hookJQuery);
})();
