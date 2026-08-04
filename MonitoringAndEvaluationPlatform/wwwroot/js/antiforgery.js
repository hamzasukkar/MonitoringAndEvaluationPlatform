/*
 * Attaches the ASP.NET Core antiforgery token to every same-origin, state-changing
 * AJAX request.
 *
 * Why this exists: antiforgery validation is applied globally by
 * AutoValidateAntiforgeryTokenAttribute, but roughly eighteen view/script files issue
 * $.ajax / fetch POSTs without sending a token. Rather than editing each call site,
 * this hooks jQuery's ajax prefilter and window.fetch once, in every layout.
 *
 * The header name matches AddAntiforgery(o => o.HeaderName = "RequestVerificationToken")
 * in Program.cs. Existing hand-rolled token code in guide-editor.js and Admin/Users.cshtml
 * keeps working - a request that already carries the header is left alone.
 */
(function () {
    'use strict';

    var HEADER_NAME = 'RequestVerificationToken';

    function getToken() {
        // Rendered by the _AntiforgeryToken partial in each layout.
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : null;
    }

    function needsToken(method) {
        if (!method) {
            return false;
        }
        var m = method.toUpperCase();
        return m !== 'GET' && m !== 'HEAD' && m !== 'OPTIONS' && m !== 'TRACE';
    }

    function isSameOrigin(url) {
        if (!url) {
            // Relative to the current document.
            return true;
        }
        try {
            return new URL(url, window.location.href).origin === window.location.origin;
        } catch (e) {
            return false;
        }
    }

    // --- jQuery ---------------------------------------------------------------
    if (window.jQuery) {
        window.jQuery.ajaxPrefilter(function (options, originalOptions, jqXHR) {
            if (!needsToken(options.type) || !isSameOrigin(options.url)) {
                return;
            }

            var token = getToken();
            if (token) {
                jqXHR.setRequestHeader(HEADER_NAME, token);
            }
        });
    }

    // --- fetch ---------------------------------------------------------------
    if (window.fetch) {
        var originalFetch = window.fetch;

        window.fetch = function (input, init) {
            init = init || {};

            var url = typeof input === 'string' ? input : (input && input.url);
            var method = init.method || (input && input.method) || 'GET';

            if (needsToken(method) && isSameOrigin(url)) {
                var token = getToken();
                if (token) {
                    var headers = new Headers(init.headers || (input && input.headers) || {});
                    if (!headers.has(HEADER_NAME)) {
                        headers.set(HEADER_NAME, token);
                    }
                    init = Object.assign({}, init, { headers: headers });
                }
            }

            return originalFetch.call(this, input, init);
        };
    }
})();
