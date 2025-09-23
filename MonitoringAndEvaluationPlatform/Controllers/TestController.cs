using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class TestController : Controller
    {
        [AllowAnonymous]
        public IActionResult Public()
        {
            return View();
        }

        [Authorize]
        public IActionResult AuthOnly()
        {
            return View();
        }

        [Authorize(Roles = "SystemAdministrator")]
        public IActionResult AdminOnly()
        {
            return View();
        }
    }
}