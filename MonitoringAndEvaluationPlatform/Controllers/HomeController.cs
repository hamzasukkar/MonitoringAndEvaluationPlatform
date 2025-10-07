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
            var frameworks = await _context.Frameworks.ToListAsync();
            var ministries = await _context.Ministries.ToListAsync();
            var governorates = await _context.Governorates.ToListAsync();

            var frameworksPerformance = frameworks.Select(f => new FrameworkPerformanceViewModel
            {
                Code = f.Code,
                Name = f.Name,
                IndicatorsPerformance = f.IndicatorsPerformance,
                DisbursementPerformance = f.DisbursementPerformance,
                FieldMonitoring = f.FieldMonitoring,
                ImpactAssessment = f.ImpactAssessment,
                // Calculate overall performance as average of all performance metrics
                OverallPerformance = (f.IndicatorsPerformance + f.DisbursementPerformance +
                                     f.FieldMonitoring + f.ImpactAssessment) / 4.0
            }).ToList();

            // Get projects by ministry count
            var projectsByMinistry = await _context.Projects
                .Include(p => p.Ministries)
                .SelectMany(p => p.Ministries.Select(m => new { Ministry = m }))
                .GroupBy(x => x.Ministry.MinistryDisplayName ?? x.Ministry.MinistryUserName)
                .Select(g => new { Ministry = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Ministry, x => x.Count);

            // Get projects by governorate count
            var projectsByGovernorate = await _context.Projects
                .Include(p => p.Governorates)
                .SelectMany(p => p.Governorates.Select(g => new { Governorate = g }))
                .GroupBy(x => x.Governorate.EN_Name ?? x.Governorate.AR_Name)
                .Select(g => new { Governorate = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Governorate, x => x.Count);

            // Calculate monthly performance (based on project completion over the year)
            var currentYear = DateTime.Now.Year;
            var projects = await _context.Projects
                .Where(p => p.StartDate.Year <= currentYear && p.EndDate.Year >= currentYear)
                .ToListAsync();

            var monthlyPerformance = new List<MonthlyPerformanceViewModel>();
            for (int month = 1; month <= 12; month++)
            {
                var monthDate = new DateTime(currentYear, month, 1);
                var completedProjects = projects.Count(p => p.EndDate <= monthDate && p.StartDate <= monthDate);
                var activeProjects = projects.Count(p => p.StartDate <= monthDate);

                var projectImplementation = activeProjects > 0 ? (completedProjects * 100.0 / activeProjects) : 0;

                // Calculate performance indicators based on project performance
                var avgPerformance = projects
                    .Where(p => p.StartDate <= monthDate)
                    .Average(p => (double?)p.performance) ?? 0;

                monthlyPerformance.Add(new MonthlyPerformanceViewModel
                {
                    Month = monthDate.ToString("MMM"),
                    ProjectImplementation = Math.Round(projectImplementation, 2),
                    PerformanceIndicators = Math.Round(avgPerformance, 2)
                });
            }

            // Get recent activities based on recent projects
            var recentActivities = new List<RecentActivityViewModel>();
            var recentProjects = await _context.Projects
                .OrderByDescending(p => p.StartDate)
                .Take(5)
                .ToListAsync();

            foreach (var project in recentProjects)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    ActivityType = "project",
                    ActivityTitle = $"Project '{project.ProjectName}' started",
                    ActivityDate = project.StartDate,
                    Icon = "fa-project-diagram"
                });
            }

            var viewModel = new DashboardSummaryViewModel
            {
                TotalFrameworks = frameworks.Count,
                Frameworks = frameworks.Take(5).ToList(),
                FrameworksPerformance = frameworksPerformance,
                TotlalMinistries = ministries.Count,
                Ministries = ministries.Take(5).ToList(),
                ProjectsByMinistry = projectsByMinistry,
                TotalProjects = await _context.Projects.CountAsync(),
                Projects = await _context.Projects
                    .Include(p => p.Sectors)
                    .Include(p => p.Ministries)
                    .Include(p => p.Donors)
                    .Take(5)
                    .ToListAsync(),
                TotalGovernorate = governorates.Count,
                Governorates = governorates.Take(5).ToList(),
                ProjectsByGovernorate = projectsByGovernorate,
                Districts = await _context.Districts.Take(10).ToListAsync(),
                SubDistricts = await _context.SubDistricts.Take(10).ToListAsync(),
                Communities = await _context.Communities.Take(10).ToListAsync(),
                MonthlyPerformance = monthlyPerformance,
                RecentActivities = recentActivities.OrderByDescending(a => a.ActivityDate).ToList()
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
