using Microsoft.AspNetCore.Mvc;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class ThemeController : Controller
    {
        private readonly IThemeService _themeService;

        public ThemeController(IThemeService themeService)
        {
            _themeService = themeService;
        }

        [HttpPost]
        public IActionResult SetTheme(string themeId)
        {
            try
            {
                _themeService.SetTheme(HttpContext, themeId);
                
                if (Request.Headers.ContainsKey("X-Requested-With") && 
                    Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    // AJAX request
                    return Json(new { success = true, message = "Theme changed successfully" });
                }
                
                // Regular form submission - redirect back
                var returnUrl = Request.Headers["Referer"].FirstOrDefault() ?? Url.Action("Index", "Home");
                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                if (Request.Headers.ContainsKey("X-Requested-With") && 
                    Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Failed to change theme" });
                }
                
                TempData["ErrorMessage"] = "Failed to change theme";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult GetCurrentTheme()
        {
            var currentTheme = _themeService.GetCurrentTheme(HttpContext);
            return Json(currentTheme);
        }

        [HttpGet]
        public IActionResult GetAvailableThemes()
        {
            var themes = _themeService.GetAvailableThemes();
            return Json(themes);
        }
    }
}