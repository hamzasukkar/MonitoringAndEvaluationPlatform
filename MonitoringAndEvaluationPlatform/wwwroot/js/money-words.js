/*
 * money-words.js — platform-wide "number in Arabic words" hints for money fields.
 *
 * Behaviour:
 *   Display amounts  -> shown as a HOVER tooltip (no layout change). Any standalone
 *                       grouped amount ($1,234,567 / 1,234,567 / "1,234,567 (USD)") is
 *                       auto-detected; you can also force it with class "money-words"
 *                       and override the value with data-words-value="1200000".
 *   Input amounts    -> live floating hint while typing on inputs/contenteditable
 *                       carrying class "money-words-input".
 *
 * Opt out of an element with data-no-words. No jQuery dependency.
 */
(function () {
    'use strict';

    // ----- number -> Arabic words (mirrors ArabicNumberHelper / the budget field) -----
    function toArabicWords(num) {
        if (!isFinite(num) || num < 1000) {
            return '';
        }
        num = Math.floor(num);
        var billions = Math.floor(num / 1000000000);
        var afterBillions = num % 1000000000;
        var millions = Math.floor(afterBillions / 1000000);
        var afterMillions = afterBillions % 1000000;
        var thousands = Math.floor(afterMillions / 1000);
        var remainder = afterMillions % 1000;

        var parts = [];
        if (billions > 0) {
            parts.push(billions === 1 ? 'مليار' : billions + ' مليار');
        }
        if (millions > 0) {
            parts.push(millions === 1 ? 'مليون' : millions + ' مليون');
        }
        if (thousands > 0) {
            if (thousands === 1) {
                parts.push('ألف');
            } else if (thousands === 10) {
                parts.push('عشرة آلاف');
            } else {
                parts.push(thousands + ' ألف');
            }
        }
        if (remainder > 0) {
            parts.push(remainder.toString());
        }
        return parts.join(' و');
    }

    // Normalize Arabic-Indic digits and Arabic separators to ASCII so amounts rendered in
    // Arabic culture (e.g. "١٬٢٣٤" or "1٬234") are understood the same as "1,234".
    function normalizeNum(s) {
        return String(s == null ? '' : s)
            .replace(/[٠-٩]/g, function (d) { return d.charCodeAt(0) - 0x0660; })  // Arabic-Indic 0-9
            .replace(/[۰-۹]/g, function (d) { return d.charCodeAt(0) - 0x06F0; })  // Extended Arabic-Indic
            .replace(/٬/g, ',')   // Arabic thousands separator -> comma
            .replace(/٫/g, '.');  // Arabic decimal separator -> dot
    }

    function numFromText(raw) {
        if (raw == null) return NaN;
        var cleaned = normalizeNum(raw).replace(/[^\d.]/g, '');
        if (cleaned === '' || cleaned === '.') return NaN;
        return parseFloat(cleaned);
    }

    window.MoneyWords = { toArabicWords: toArabicWords };

    // ----- one-time styles -----
    function injectStyles() {
        if (document.getElementById('money-words-styles')) return;
        var css =
            '.money-words-hint{position:fixed;z-index:3000;background:#0d6efd;color:#fff;padding:4px 10px;' +
            'border-radius:8px;font-size:.8rem;font-weight:600;direction:rtl;box-shadow:0 4px 12px rgba(0,0,0,.15);' +
            'pointer-events:none;white-space:nowrap;display:none;}';
        var style = document.createElement('style');
        style.id = 'money-words-styles';
        style.textContent = css;
        document.head.appendChild(style);
    }

    // ----- mark display elements as hoverable (NO inserted text -> no layout shift) -----
    function wordsFor(el) {
        var attr = el.getAttribute('data-words-value');
        return toArabicWords(numFromText(attr != null ? attr : el.textContent));
    }

    function markDisplay(el) {
        if (wordsFor(el)) {
            el.classList.add('money-words-has');
        } else {
            el.classList.remove('money-words-has');
        }
    }

    function initDisplays(root) {
        var nodes = (root || document).querySelectorAll('.money-words');
        for (var i = 0; i < nodes.length; i++) markDisplay(nodes[i]);
    }

    // ----- automatic detection of standalone money amounts -----
    var MONEY_ONLY = /^\s*(?:\$|€|£|SYP|USD|EUR)?\s*\d{1,3}(?:,\d{3})+(?:\.\d+)?\s*(?:\(?\s*(?:USD|EUR|SYP|\$|€)\s*\)?)?\s*$/;

    function tagIfMoney(el) {
        if (!el || el.nodeType !== 1) return;
        if (el.childElementCount !== 0) return;                 // leaf nodes only
        if (el.classList.contains('money-words') ||
            el.classList.contains('money-words-has')) return;   // already handled
        if (el.hasAttribute('data-mw-auto')) return;            // already processed
        if (el.hasAttribute('data-no-words')) return;           // explicit opt-out
        var tag = el.tagName;
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' ||
            tag === 'OPTION' || tag === 'SCRIPT' || tag === 'STYLE') return;
        if (el.isContentEditable) return;
        var txt = normalizeNum((el.textContent || '').trim());
        if (!MONEY_ONLY.test(txt)) return;
        // Skip amounts that sit inside a formula/expression (e.g. "($X ÷ $Y) × 100")
        var parent = el.parentElement;
        if (parent && /[÷×=]/.test(parent.textContent)) return;
        el.setAttribute('data-mw-auto', '1');
        markDisplay(el);
    }

    function autoDetect(root) {
        var scope = (root && root.querySelectorAll) ? root : document;
        if (scope.nodeType === 1) tagIfMoney(scope);
        var els = scope.querySelectorAll('*');
        for (var i = 0; i < els.length; i++) tagIfMoney(els[i]);
    }

    // ----- shared floating hint (hover for display, live for inputs) -----
    var hintEl = null;
    function ensureHint() {
        if (hintEl) return hintEl;
        hintEl = document.createElement('div');
        hintEl.className = 'money-words-hint';
        document.body.appendChild(hintEl);
        return hintEl;
    }
    function rawOf(el) {
        if (el.hasAttribute && el.hasAttribute('data-words-value')) return el.getAttribute('data-words-value');
        return (typeof el.value === 'string') ? el.value : el.textContent;
    }
    function showHint(el) {
        var h = ensureHint();
        var words = toArabicWords(numFromText(rawOf(el)));
        if (!words) { h.style.display = 'none'; return; }
        var rect = el.getBoundingClientRect();
        h.textContent = words;
        h.style.top = (rect.bottom + 4) + 'px';
        h.style.left = rect.left + 'px';
        h.style.display = 'block';
    }
    function hideHint() { if (hintEl) hintEl.style.display = 'none'; }

    function isInput(el) {
        return el && el.classList && el.classList.contains('money-words-input');
    }
    function isHoverMoney(el) {
        return el && el.classList &&
            (el.classList.contains('money-words-has') || el.classList.contains('money-words'));
    }

    // inputs: live hint while typing
    document.addEventListener('focusin', function (e) { if (isInput(e.target)) showHint(e.target); });
    document.addEventListener('focusout', function (e) { if (isInput(e.target)) hideHint(); });
    document.addEventListener('input', function (e) { if (isInput(e.target)) showHint(e.target); });

    // hint on mouse hover — for displays AND inputs (consistent everywhere)
    document.addEventListener('mouseover', function (e) {
        if (isHoverMoney(e.target) || isInput(e.target)) showHint(e.target);
    });
    document.addEventListener('mouseout', function (e) {
        if (isInput(e.target) && document.activeElement === e.target) return; // keep visible while typing
        if (isHoverMoney(e.target) || isInput(e.target)) hideHint();
    });
    window.addEventListener('scroll', function () { hideHint(); }, true);

    // ----- bootstrap + watch dynamic content -----
    function start() {
        injectStyles();
        initDisplays(document);
        autoDetect(document);
        if (!window.MutationObserver) return;
        var mo = new MutationObserver(function (muts) {
            for (var i = 0; i < muts.length; i++) {
                var added = muts[i].addedNodes;
                for (var j = 0; added && j < added.length; j++) {
                    var n = added[j];
                    if (n.nodeType !== 1) continue;
                    if (n.classList && n.classList.contains('money-words')) markDisplay(n);
                    if (n.querySelectorAll) { initDisplays(n); autoDetect(n); }
                    else tagIfMoney(n);
                }
            }
        });
        mo.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
