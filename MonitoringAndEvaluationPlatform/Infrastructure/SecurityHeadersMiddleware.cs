namespace MonitoringAndEvaluationPlatform.Infrastructure
{
    /// <summary>
    /// Adds the standard security response headers. The application previously sent none of
    /// them - no CSP, no framing protection, no MIME-sniffing protection, no referrer policy.
    ///
    /// Registered before UseStaticFiles so the headers also cover static assets.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bool _cspEnforced;
        private readonly string _cspPolicy;

        public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;

            // Content-Security-Policy ships in report-only mode by default.
            //
            // This codebase is not CSP-ready: several views carry thousands of lines of inline
            // <script>, inline onclick= handlers are widespread, and a number of CDN references
            // have no SRI. Enforcing a policy now would blank those pages. Report-only lets
            // violations be collected first; flip Security:EnforceCsp once the inline script
            // has been moved out or nonces added.
            _cspEnforced = configuration.GetValue<bool>("Security:EnforceCsp");

            _cspPolicy = string.Join("; ",
                "default-src 'self'",
                // 'unsafe-inline'/'unsafe-eval' are required by the current views; removing
                // them is the point of the report-only rollout.
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://code.jquery.com https://cdnjs.cloudflare.com https://unpkg.com",
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com",
                "font-src 'self' data: https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.gstatic.com",
                "img-src 'self' data: blob: https:",
                "connect-src 'self'",
                // No plugins, and no framing of this site by anyone.
                "object-src 'none'",
                "frame-ancestors 'none'",
                "base-uri 'self'",
                "form-action 'self'");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Stop the browser re-interpreting a response as a type it was not sent as -
            // relevant wherever user-uploaded files are served.
            headers["X-Content-Type-Options"] = "nosniff";

            // Clickjacking. frame-ancestors in the CSP is the modern equivalent; this covers
            // browsers that only honour the legacy header.
            headers["X-Frame-Options"] = "DENY";

            // Do not leak full URLs (which carry record IDs) to third-party sites.
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), payment=(), usb=()";

            headers[_cspEnforced ? "Content-Security-Policy" : "Content-Security-Policy-Report-Only"] = _cspPolicy;

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
            app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
