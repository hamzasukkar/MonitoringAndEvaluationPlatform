using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class DebugController : Controller
    {
        public IActionResult UserInfo()
        {
            var user = HttpContext.User;

            ViewBag.IsAuthenticated = user.Identity?.IsAuthenticated;
            ViewBag.UserName = user.Identity?.Name;
            ViewBag.Claims = user.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
            ViewBag.Roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return View();
        }
    }
}