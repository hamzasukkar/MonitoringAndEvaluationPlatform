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
            var donors = await _context.Donors.ToListAsync();
            var sectors = await _context.Sectors.ToListAsync();

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

            var donorsPerformance = donors.Select(d => new DonorPerformanceViewModel
            {
                Code = d.Code,
                Partner = d.Partner,
                IndicatorsPerformance = d.IndicatorsPerformance,
                DisbursementPerformance = d.DisbursementPerformance,
                FieldMonitoring = d.FieldMonitoring,
                ImpactAssessment = d.ImpactAssessment,
                // Calculate overall performance as average of all performance metrics
                OverallPerformance = (d.IndicatorsPerformance + d.DisbursementPerformance +
                                     d.FieldMonitoring + d.ImpactAssessment) / 4.0
            }).ToList();

            var sectorsPerformance = sectors.Select(s => new SectorPerformanceViewModel
            {
                Code = s.Code,
                Name = s.EN_Name ?? s.AR_Name,
                IndicatorsPerformance = s.IndicatorsPerformance,
                DisbursementPerformance = s.DisbursementPerformance,
                FieldMonitoring = s.FieldMonitoring,
                ImpactAssessment = s.ImpactAssessment,
                // Calculate overall performance as average of all performance metrics
                OverallPerformance = (s.IndicatorsPerformance + s.DisbursementPerformance +
                                     s.FieldMonitoring + s.ImpactAssessment) / 4.0
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

            // Get projects by donor count
            var projectsByDonor = await _context.Projects
                .Include(p => p.Donors)
                .SelectMany(p => p.Donors.Select(d => new { Donor = d }))
                .GroupBy(x => x.Donor.Partner)
                .Select(g => new { Donor = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Donor, x => x.Count);

            // Get projects by sector count
            var projectsBySector = await _context.Projects
                .Include(p => p.Sectors)
                .SelectMany(p => p.Sectors.Select(s => new { Sector = s }))
                .GroupBy(x => x.Sector.EN_Name ?? x.Sector.AR_Name)
                .Select(g => new { Sector = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Sector, x => x.Count);

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

            // Get all projects performance data (top 10 for chart display)
            var allProjects = await _context.Projects.ToListAsync();
            var projectsPerformance = allProjects
                .OrderByDescending(p => p.performance)
                .Take(10)
                .Select(p => new ProjectPerformanceViewModel
                {
                    ProjectID = p.ProjectID,
                    ProjectName = p.ProjectName,
                    Performance = p.performance,
                    DisbursementPerformance = p.DisbursementPerformance,
                    FieldMonitoring = p.FieldMonitoring,
                    ImpactAssessment = p.ImpactAssessment
                })
                .ToList();

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
                RecentActivities = recentActivities.OrderByDescending(a => a.ActivityDate).ToList(),
                ProjectsByDonor = projectsByDonor,
                DonorsPerformance = donorsPerformance,
                ProjectsBySector = projectsBySector,
                SectorsPerformance = sectorsPerformance,
                ProjectsPerformance = projectsPerformance
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
