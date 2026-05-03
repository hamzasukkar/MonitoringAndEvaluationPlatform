// Lightweight client-side form-draft persistence using localStorage.
// Saves the form on every input/change (debounced) and restores it on page load.
// Cleared explicitly by the redirect target via FormDraft.clearKey(...) once the
// server confirms the submission succeeded.
(function (window, document) {
    'use strict';

    var DEFAULT_MAX_AGE_MS = 24 * 60 * 60 * 1000;
    var DEFAULT_DEBOUNCE_MS = 400;

    function FormDraft(opts) {
        opts = opts || {};
        this.form = typeof opts.form === 'string' ? document.querySelector(opts.form) : opts.form;
        this.key = opts.key;
        this.maxAge = opts.maxAge || DEFAULT_MAX_AGE_MS;
        this.debounceMs = opts.debounce || DEFAULT_DEBOUNCE_MS;
        this.skipNames = opts.skipNames || ['__RequestVerificationToken'];
        this.restoredText = opts.restoredText || 'Unsaved draft restored';
        this.discardText = opts.discardText || 'Discard draft';
        this._timer = null;
    }

    FormDraft.prototype.init = function () {
        if (!this.form || !this.key) return false;

        var hadDraft = this._tryRestore();

        var self = this;
        var handler = function () {
            if (self._timer) clearTimeout(self._timer);
            self._timer = setTimeout(function () { self.save(); }, self.debounceMs);
        };
        this.form.addEventListener('input', handler, true);
        this.form.addEventListener('change', handler, true);

        return hadDraft;
    };

    FormDraft.prototype._elements = function () {
        return this.form.querySelectorAll('input, select, textarea');
    };

    FormDraft.prototype._isSkippableType = function (t) {
        return t === 'file' || t === 'submit' || t === 'button' || t === 'reset' || t === 'password' || t === 'image';
    };

    FormDraft.prototype.save = function () {
        try {
            var data = {};
            var els = this._elements();
            for (var i = 0; i < els.length; i++) {
                var el = els[i];
                if (!el.name || this.skipNames.indexOf(el.name) !== -1) continue;
                var t = (el.type || '').toLowerCase();
                if (this._isSkippableType(t)) continue;

                if (t === 'checkbox' || t === 'radio') {
                    if (!data[el.name]) data[el.name] = { __cb: true, values: [] };
                    if (el.checked) data[el.name].values.push(el.value);
                } else if (el.tagName === 'SELECT' && el.multiple) {
                    var vals = [];
                    for (var j = 0; j < el.options.length; j++) {
                        if (el.options[j].selected) vals.push(el.options[j].value);
                    }
                    data[el.name] = { __ms: true, values: vals };
                } else {
                    data[el.name] = el.value;
                }
            }
            localStorage.setItem(this.key, JSON.stringify({ ts: Date.now(), data: data }));
        } catch (e) {
            // localStorage may be disabled or full — silently ignore
        }
    };

    FormDraft.prototype._tryRestore = function () {
        var raw;
        try { raw = localStorage.getItem(this.key); } catch (e) { return false; }
        if (!raw) return false;

        var payload;
        try { payload = JSON.parse(raw); } catch (e) { this.clear(); return false; }
        if (!payload || !payload.data) return false;
        if (Date.now() - payload.ts > this.maxAge) { this.clear(); return false; }

        this._apply(payload.data);
        this._showIndicator(payload.ts);
        return true;
    };

    FormDraft.prototype._apply = function (data) {
        var els = this._elements();
        for (var i = 0; i < els.length; i++) {
            var t = (els[i].type || '').toLowerCase();
            if (t === 'checkbox' || t === 'radio') els[i].checked = false;
        }
        for (var i = 0; i < els.length; i++) {
            var el = els[i];
            if (!el.name || this.skipNames.indexOf(el.name) !== -1) continue;
            var saved = data[el.name];
            if (saved === undefined) continue;
            var t = (el.type || '').toLowerCase();
            if (this._isSkippableType(t)) continue;

            if (t === 'checkbox' || t === 'radio') {
                if (saved && saved.__cb && saved.values && saved.values.indexOf(el.value) !== -1) {
                    el.checked = true;
                }
            } else if (el.tagName === 'SELECT' && el.multiple && saved && saved.__ms) {
                for (var j = 0; j < el.options.length; j++) {
                    el.options[j].selected = saved.values.indexOf(el.options[j].value) !== -1;
                }
            } else if (typeof saved === 'string') {
                el.value = saved;
            }
            try {
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
            } catch (e) {
                var ev = document.createEvent('HTMLEvents');
                ev.initEvent('change', true, false);
                el.dispatchEvent(ev);
            }
        }
    };

    FormDraft.prototype._showIndicator = function (ts) {
        var diff = Date.now() - ts;
        var label;
        if (diff < 60000) label = 'just now';
        else if (diff < 3600000) label = Math.round(diff / 60000) + ' min ago';
        else if (diff < 86400000) label = Math.round(diff / 3600000) + ' hr ago';
        else label = Math.round(diff / 86400000) + ' day(s) ago';

        var div = document.createElement('div');
        div.className = 'alert alert-info d-flex align-items-center justify-content-between mb-3 form-draft-indicator';
        div.setAttribute('role', 'alert');
        div.innerHTML =
            '<span><i class="fas fa-history me-2"></i>' + this.restoredText +
            ' <small class="text-muted">(' + label + ')</small></span>' +
            '<button type="button" class="btn btn-sm btn-outline-secondary form-draft-discard">' +
            '<i class="fas fa-trash-alt me-1"></i>' + this.discardText + '</button>';
        if (this.form && this.form.parentNode) {
            this.form.parentNode.insertBefore(div, this.form);
        }
        var self = this;
        var btn = div.querySelector('.form-draft-discard');
        if (btn) {
            btn.addEventListener('click', function () {
                self.clear();
                window.location.reload();
            });
        }
    };

    FormDraft.prototype.clear = function () {
        try { localStorage.removeItem(this.key); } catch (e) {}
    };

    FormDraft.clearKey = function (key) {
        try { if (key) localStorage.removeItem(key); } catch (e) {}
    };

    window.FormDraft = FormDraft;
})(window, document);
