// Adds a fullscreen toggle to every Chart.js chart on the page. This is generic: it does not know
// about any specific page's markup, so no view needs to be touched to get the feature - it just
// needs this file (and chart-fullscreen.css) loaded after Chart.js.
//
// A chart's "host" is the nearest ancestor whose class name looks like a chart box
// (chart-container / chart-card / chart-content / chart-wrapper, in any combination, e.g.
// "distribution-chart-wrapper" or "chart-container-card"), or the canvas's own parent when no
// such ancestor exists. Fullscreen pins that host over the page (see chart-fullscreen.css).
//
// The host is reparented to <body> while fullscreen, then moved straight back on close (a
// placeholder marks the spot). This app's "glass card" styling puts backdrop-filter/blur on most
// card wrappers, and a backdrop-filter (like a transform) makes its element the containing block
// for any position:fixed descendant - so a chart nested inside one would only ever go "fixed"
// relative to that small card, not the viewport. Moving the host out from under every such
// ancestor is what makes position:fixed (see chart-fullscreen.css) actually fill the screen.
(function () {
    'use strict';

    var ACTIVE_CLASS = 'chart-fs-active';
    var HOST_ATTR = 'data-fs-host';
    var HOST_SELECTOR_RE = /chart-(container|card|content|wrapper|box)/;
    var activePlaceholder = null;

    var isRtl = document.documentElement.getAttribute('dir') === 'rtl' ||
        (document.documentElement.getAttribute('lang') || '').startsWith('ar');
    var LABEL_EXPAND = isRtl ? 'عرض بملء الشاشة' : 'View fullscreen';
    var LABEL_COLLAPSE = isRtl ? 'إغلاق ملء الشاشة' : 'Exit fullscreen';

    var backdrop = null;
    var activeHost = null;

    function ensureBackdrop() {
        if (backdrop) return backdrop;
        backdrop = document.createElement('div');
        backdrop.className = 'chart-fs-backdrop';
        backdrop.addEventListener('click', closeActive);
        document.body.appendChild(backdrop);
        return backdrop;
    }

    function chartsWithin(host) {
        var out = [];
        host.querySelectorAll('canvas').forEach(function (c) {
            var chart = window.Chart && window.Chart.getChart ? window.Chart.getChart(c) : null;
            if (chart) out.push(chart);
        });
        return out;
    }

    function resizeCharts(host) {
        // Two passes: Chart.js needs the host's new box to exist before it can measure it, and
        // that box can still be settling (CSS transition, reflow) a frame after the class flips.
        chartsWithin(host).forEach(function (c) { c.resize(); });
        requestAnimationFrame(function () {
            chartsWithin(host).forEach(function (c) { c.resize(); });
        });
    }

    function setButtonState(host, expanded) {
        var btn = host.querySelector('.chart-fullscreen-btn');
        if (!btn) return;
        btn.innerHTML = '<i class="fas fa-' + (expanded ? 'compress' : 'expand') + '"></i>';
        var label = expanded ? LABEL_COLLAPSE : LABEL_EXPAND;
        btn.setAttribute('title', label);
        btn.setAttribute('aria-label', label);
        btn.setAttribute('aria-pressed', expanded ? 'true' : 'false');
    }

    function closeActive() {
        if (!activeHost) return;
        var host = activeHost;
        activeHost = null;
        host.classList.remove(ACTIVE_CLASS);
        setButtonState(host, false);
        if (backdrop) backdrop.classList.remove('show');
        if (activePlaceholder) {
            activePlaceholder.replaceWith(host);
            activePlaceholder = null;
        }
        resizeCharts(host);
    }

    function openHost(host) {
        if (activeHost === host) { closeActive(); return; }
        if (activeHost) closeActive();
        ensureBackdrop().classList.add('show');
        activePlaceholder = document.createComment('chart-fs-placeholder');
        host.replaceWith(activePlaceholder);
        document.body.appendChild(host);
        host.classList.add(ACTIVE_CLASS);
        activeHost = host;
        setButtonState(host, true);
        resizeCharts(host);
    }

    function locateHost(canvas) {
        // The outermost matching ancestor that still contains only this one canvas - climbing
        // past it would start pulling in a section that holds a whole grid of other charts too
        // (e.g. a dashboard's "gauges" grid), which is a section, not a single chart's box.
        // Everything walked over on the way there (its title, its own fixed-height chart box,
        // any scroll wrapper, ...) is the "fill path": it still has to relay the extra height
        // fullscreen gives the host down to the canvas, or the host just grows into empty space.
        var path = [];
        var host = canvas.parentElement;
        var hostIndex = -1;
        var el = canvas.parentElement;
        var hops = 0;
        while (el && el !== document.body && hops < 8) {
            if (el.querySelectorAll('canvas').length > 1) break;
            path.push(el);
            if (HOST_SELECTOR_RE.test(el.className || '')) { host = el; hostIndex = path.length - 1; }
            el = el.parentElement;
            hops++;
        }
        return { host: host, fillPath: hostIndex >= 0 ? path.slice(0, hostIndex) : [] };
    }

    function attach(canvas) {
        if (canvas.dataset.fsProcessed) return;
        // Wait until Chart.js has actually initialized on this canvas (it may be created later,
        // or destroyed and rebuilt on the same canvas by the page's own tab/level switching) -
        // the periodic scan below keeps retrying until then.
        var chart = window.Chart && window.Chart.getChart ? window.Chart.getChart(canvas) : null;
        if (!chart) return;
        canvas.dataset.fsProcessed = '1';

        var located = locateHost(canvas);
        var host = located.host;
        if (!host || host.hasAttribute(HOST_ATTR)) return; // a shared host already has a button
        host.setAttribute(HOST_ATTR, '1');
        if (getComputedStyle(host).position === 'static') host.classList.add('chart-fs-positioned');
        located.fillPath.forEach(function (el) { el.classList.add('chart-fs-fill'); });

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'chart-fullscreen-btn no-print';
        btn.innerHTML = '<i class="fas fa-expand"></i>';
        btn.title = LABEL_EXPAND;
        btn.setAttribute('aria-label', LABEL_EXPAND);
        btn.setAttribute('aria-pressed', 'false');
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            openHost(host);
        });
        host.appendChild(btn);
    }

    function scan(root) {
        (root || document).querySelectorAll('canvas').forEach(attach);
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeActive();
    });

    document.addEventListener('DOMContentLoaded', function () {
        scan();

        // Catches canvases created later (modals, AJAX-loaded tabs, category report charts, ...).
        var observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (m) {
                m.addedNodes.forEach(function (node) {
                    if (node.nodeType !== 1) return;
                    if (node.tagName === 'CANVAS') attach(node);
                    else if (node.querySelectorAll) scan(node);
                });
            });
        });
        observer.observe(document.body, { childList: true, subtree: true });

        // Backstop for what the observer can't see: a chart built on a canvas that was already
        // sitting in the DOM (e.g. inside a Bootstrap modal, or a "view chart" panel) - no node
        // is added when that happens, so there's no mutation to catch, and it can happen any time
        // after load, not just in the first few seconds. Left running for the page's lifetime;
        // checking a page's canvases against Chart.getChart every couple of seconds is cheap.
        setInterval(scan, 2000);
    });
})();
