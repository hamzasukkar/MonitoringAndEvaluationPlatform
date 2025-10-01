using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardSummaryViewModel
            {
                TotalFrameworks = await _context.Frameworks.CountAsync(),
                Frameworks = await _context.Frameworks.Take(5).ToListAsync(),
                TotlalMinistries = await _context.Ministries.CountAsync(),
                Ministries = await _context.Ministries.Take(5).ToListAsync(),
                TotalProjects = await _context.Projects.CountAsync(),
                Projects = await _context.Projects
                    .Include(p => p.Sectors)
                    .Include(p => p.Ministries)
                    .Include(p => p.Donors)
                    .Take(5)
                    .ToListAsync(),
                TotalGovernorate = await _context.Governorates.CountAsync(),
                Governorates = await _context.Governorates.Take(5).ToListAsync(),
                Districts = await _context.Districts.Take(10).ToListAsync(),
                SubDistricts = await _context.SubDistricts.Take(10).ToListAsync(),
                Communities = await _context.Communities.Take(10).ToListAsync()
            };
            
            return View(viewModel);
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            //To Fix
            //return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            return View();
        }
    }
}
