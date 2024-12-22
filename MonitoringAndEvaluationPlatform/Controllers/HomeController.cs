using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Test()
        {
            return View();
        }

        public IActionResult Home()
        {
            return View();
        }

        [HttpGet]
        public IActionResult LoadRecentPosts(int count = 2)
        {
            ViewData["count"] = count;
            return PartialView("_RecentPostsPartial");
        }

        [HttpGet]
        public IActionResult LoadRecentPosts2(int count = 2)
        {
            ViewData["count"] = count;
            return PartialView("_RecentPostsPartial2");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
