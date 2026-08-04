using Microsoft.AspNetCore.Identity;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Infrastructure
{
    /// <summary>
    /// Redirects authenticated users whose account is flagged <see cref="ApplicationUser.MustChangePassword"/>
    /// to the change-password page until they set their own password.
    ///
    /// This is what neutralises server-chosen passwords: seeded accounts and accounts created
    /// by an administrator all start flagged, so a shared or admin-known password cannot be
    /// used to keep operating the account.
    /// </summary>
    public class PasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Paths the flagged user must still reach: the change-password page itself, sign-out,
        /// and the static assets those pages need.
        /// </summary>
        private static readonly string[] AllowedPaths =
        {
            "/Identity/Account/Manage/ChangePassword",
            "/Identity/Account/Logout",
            "/Identity/Account/AccessDenied",
            "/css",
            "/js",
            "/lib",
            "/images",
            "/favicon.ico"
        };

        public PasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated != true || IsAllowedPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var user = await userManager.GetUserAsync(context.User);
            if (user?.MustChangePassword == true)
            {
                // Answer AJAX callers with a status code rather than an HTML redirect they
                // cannot follow, so the client sees a real failure instead of parsing a page.
                if (IsAjaxRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                context.Response.Redirect("/Identity/Account/Manage/ChangePassword");
                return;
            }

            await _next(context);
        }

        private static bool IsAllowedPath(PathString path) =>
            AllowedPaths.Any(allowed => path.StartsWithSegments(allowed, StringComparison.OrdinalIgnoreCase));

        private static bool IsAjaxRequest(HttpRequest request) =>
            request.Headers["X-Requested-With"] == "XMLHttpRequest"
            || request.Headers.Accept.Any(a => a != null && a.Contains("application/json", StringComparison.OrdinalIgnoreCase));
    }

    public static class PasswordChangeMiddlewareExtensions
    {
        public static IApplicationBuilder UsePasswordChangeEnforcement(this IApplicationBuilder app) =>
            app.UseMiddleware<PasswordChangeMiddleware>();
    }
}
