using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public interface IThemeService
    {
        Theme GetCurrentTheme(HttpContext httpContext);
        void SetTheme(HttpContext httpContext, string themeId);
        List<Theme> GetAvailableThemes();
        Theme GetThemeById(string themeId);
    }

    public class ThemeService : IThemeService
    {
        public Theme GetCurrentTheme(HttpContext httpContext)
        {
            var themeId = httpContext.Request.Cookies[ThemeConstants.THEME_COOKIE_NAME];
            
            if (string.IsNullOrEmpty(themeId))
            {
                return ThemeConstants.AvailableThemes.FirstOrDefault(t => t.IsDefault) 
                       ?? ThemeConstants.AvailableThemes.First();
            }
            
            return GetThemeById(themeId);
        }

        public void SetTheme(HttpContext httpContext, string themeId)
        {
            var theme = GetThemeById(themeId);
            if (theme != null)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = false, // Allow JavaScript access for theme switching
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                };
                
                httpContext.Response.Cookies.Append(ThemeConstants.THEME_COOKIE_NAME, themeId, cookieOptions);
            }
        }

        public List<Theme> GetAvailableThemes()
        {
            return ThemeConstants.AvailableThemes;
        }

        public Theme GetThemeById(string themeId)
        {
            return ThemeConstants.AvailableThemes.FirstOrDefault(t => t.Id == themeId) 
                   ?? ThemeConstants.AvailableThemes.FirstOrDefault(t => t.IsDefault);
        }
    }
}