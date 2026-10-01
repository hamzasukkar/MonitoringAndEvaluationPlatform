using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.ViewModel;
using MonitoringAndEvaluationPlatform.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGuideService _guideService;

        private readonly ICurrencyConversionService _currencyConversion;

        private readonly IMinistryScopeService _ministryScope;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IGuideService guideService, ICurrencyConversionService currencyConversion, IMinistryScopeService ministryScope)
        {
            _ministryScope = ministryScope;
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _guideService = guideService;
            _currencyConversion = currencyConversion;
        }

        private async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            var scope = await _ministryScope.GetScopeAsync();
            return (scope.IsAdmin, scope.MinistryCode);
        }

        public async Task<IActionResult> Index()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var frameworksQuery = _context.Frameworks.AsQueryable();
            var projectsQuery = _context.Projects.AsQueryable();
            if (!isAdmin)
            {
                frameworksQuery = scopedMinistryCode is null
                    ? frameworksQuery.Where(_ => false)
                    : frameworksQuery.Where(f => f.MinistryCode == scopedMinistryCode);
                projectsQuery = scopedMinistryCode is null
                    ? projectsQuery.Where(_ => false)
                    : projectsQuery.Where(p => p.MinistryCode == scopedMinistryCode);
            }

            var scope = new MinistryScope(isAdmin, scopedMinistryCode);
            var frameworks = await frameworksQuery.ToListAsync();
            var ministries = await _context.Ministries.WithinScope(scope).ToListAsync();
            var governorates = await _context.Governorates.ToListAsync();
            var donors = await _context.Donors.AsNoTracking().ToListAsync();
            var sectors = await _context.Sectors.AsNoTracking().ToListAsync();

            // The stored donor/sector performance is a national figure that blends every
            // ministry's projects. A non-admin gets it recomputed from their own projects, and only
            // for the donors/sectors those projects actually involve.
            var ownProjects = isAdmin
                ? new List<Project>()
                : await projectsQuery
                    .AsNoTracking()
                    .Include(p => p.Donors)
                    .Include(p => p.Phases)
                        .ThenInclude(pp => pp.ActionPlan)
                            .ThenInclude(ap => ap!.Plans)
                    .ToListAsync();
            var performanceConverter = await _currencyConversion.GetConverterAsync();
            (double Indicators, double Disbursement)? ScopedPerformance(List<Project> rowProjects) =>
                rowProjects.Count == 0
                    ? null
                    : (ScopedAggregates.IndicatorsPerformance(rowProjects),
                       ScopedAggregates.DisbursementPerformance(rowProjects, performanceConverter));
            if (!isAdmin)
            {
                donors = donors
                    .Select(d => (Donor: d, Perf: ScopedPerformance(ownProjects.Where(p => p.Donors.Any(x => x.Code == d.Code)).ToList())))
                    .Where(x => x.Perf != null)
                    .Select(x =>
                    {
                        x.Donor.IndicatorsPerformance = x.Perf!.Value.Indicators;
                        x.Donor.DisbursementPerformance = x.Perf.Value.Disbursement;
                        return x.Donor;
                    })
                    .ToList();
                sectors = sectors
                    .Select(s => (Sector: s, Perf: ScopedPerformance(ownProjects.Where(p => p.SectorCode == s.Code).ToList())))
                    .Where(x => x.Perf != null)
                    .Select(x =>
                    {
                        x.Sector.IndicatorsPerformance = x.Perf!.Value.Indicators;
                        x.Sector.DisbursementPerformance = x.Perf.Value.Disbursement;
                        return x.Sector;
                    })
                    .ToList();
            }

            var frameworksPerformance = frameworks.Select(f => new FrameworkPerformanceViewModel
            {
                Code = f.Code,
                Name = f.Name,
                IndicatorsPerformance = f.IndicatorsPerformance,
                DisbursementPerformance = f.DisbursementPerformance,
                OverallPerformance = (f.IndicatorsPerformance + f.DisbursementPerformance) / 2.0
            }).ToList();

            var donorsPerformance = donors.Select(d => new DonorPerformanceViewModel
            {
                Code = d.Code,
                Partner = d.Partner,
                IndicatorsPerformance = d.IndicatorsPerformance,
                DisbursementPerformance = d.DisbursementPerformance,
                OverallPerformance = (d.IndicatorsPerformance + d.DisbursementPerformance) / 2.0
            }).ToList();

            var sectorsPerformance = sectors.Select(s => new SectorPerformanceViewModel
            {
                Code = s.Code,
                Name = s.EN_Name ?? s.AR_Name,
                IndicatorsPerformance = s.IndicatorsPerformance,
                DisbursementPerformance = s.DisbursementPerformance,
                OverallPerformance = (s.IndicatorsPerformance + s.DisbursementPerformance) / 2.0
            }).ToList();

            // Get projects by ministry count, by the OWNING ministry: the ProjectMinistries mirror
            // of a legacy project can still name other ministries.
            var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var projectsByMinistry = await projectsQuery
                .Where(p => p.Ministry != null)
                .GroupBy(p => currentCulture == "ar"
                    ? (p.Ministry!.MinistryDisplayName_AR ?? p.Ministry.MinistryUserName)
                    : (p.Ministry!.MinistryDisplayName_EN ?? p.Ministry.MinistryUserName))
                .Select(g => new { Ministry = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Ministry, x => x.Count);

            // Get projects by governorate count
            var projectsByGovernorate = await projectsQuery
                .Include(p => p.Governorates)
                .SelectMany(p => p.Governorates.Select(g => new { Governorate = g }))
                .GroupBy(x => x.Governorate.EN_Name ?? x.Governorate.AR_Name)
                .Select(g => new { Governorate = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Governorate, x => x.Count);

            // Get projects by donor count
            var projectsByDonor = await projectsQuery
                .Include(p => p.Donors)
                .SelectMany(p => p.Donors.Select(d => new { Donor = d }))
                .GroupBy(x => x.Donor.Partner)
                .Select(g => new { Donor = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Donor, x => x.Count);

            // Get projects by sector count
            var projectsBySector = await projectsQuery
                .Include(p => p.Sector)
                .GroupBy(p => p.Sector.EN_Name ?? p.Sector.AR_Name)
                .Select(g => new { Sector = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Sector, x => x.Count);

            // Calculate monthly performance (based on project completion over the year)
            var currentYear = DateTime.Now.Year;
            var projects = await projectsQuery
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
            var allProjects = await projectsQuery.ToListAsync();
            var projectsPerformance = allProjects
                .OrderByDescending(p => p.performance)
                .Take(10)
                .Select(p => new ProjectPerformanceViewModel
                {
                    ProjectID = p.ProjectID,
                    ProjectName = p.ProjectName,
                    Performance = p.performance,
                    DisbursementPerformance = p.DisbursementPerformance
                })
                .ToList();

            // Prepare governorate map data with project statistics
            var governoratesMapData = new List<GovernorateMapDataViewModel>();
            // The map tooltip shows a single figure per governorate, so budgets are converted
            // to SYP here rather than mixing currencies into a meaningless sum.
            var mapConverter = await _currencyConversion.GetConverterAsync();
            foreach (var governorate in governorates)
            {
                var governorateProjects = await projectsQuery
                    .Include(p => p.Governorates)
                    .Where(p => p.Governorates.Any(g => g.Code == governorate.Code))
                    .ToListAsync();

                governoratesMapData.Add(new GovernorateMapDataViewModel
                {
                    Code = governorate.Code,
                    Name = governorate.EN_Name ?? governorate.AR_Name,
                    EN_Name = governorate.EN_Name,
                    AR_Name = governorate.AR_Name,
                    ProjectCount = governorateProjects.Count,
                    AveragePerformance = governorateProjects.Any()
                        ? Math.Round(governorateProjects.Average(p => p.performance), 2)
                        : 0,
                    TotalBudget = mapConverter.SumBudget(governorateProjects).Syp
                });
            }

            // Get recent activities based on recent projects
            var recentActivities = new List<RecentActivityViewModel>();
            var recentProjects = await projectsQuery
                .OrderByDescending(p => p.LastModifiedAt)
                .Take(5)
                .ToListAsync();

            foreach (var project in recentProjects)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    ActivityType = "project",
                    ActivityTitle = $"Project '{project.ProjectName}' edited",
                    ActivityDate = project.LastModifiedAt,
                    Icon = "fa-pen"
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
                TotalProjects = await projectsQuery.CountAsync(),
                Projects = await projectsQuery
                    .Include(p => p.Sector)
                    .Include(p => p.Ministry)
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
                ProjectsPerformance = projectsPerformance,
                GovernoratesMapData = governoratesMapData
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

        public IActionResult About()
        {
            return View();
        }

        public async Task<IActionResult> UserGuide()
        {
            var overrides = await _guideService.GetOverridesAsync();
            ViewBag.GuideOverridesJson = JsonSerializer.Serialize(overrides);
            ViewBag.CanEditGuide = User.IsInRole(UserRoles.SystemAdministrator);
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
