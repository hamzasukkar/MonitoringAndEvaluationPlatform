using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;
using System.Globalization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            if (User.IsInRole(UserRoles.SystemAdministrator))
            {
                return (true, null);
            }

            var user = await _userManager.GetUserAsync(User);
            return (false, user?.MinistryCode);
        }

        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> Index()
        {
            var viewModel = new ReportsDashboardViewModel();

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

            // Get all data with includes
            var frameworks = await frameworksQuery
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.Project)
                .ToListAsync();

            var projects = await projectsQuery
                .Include(p => p.Sectors)
                .Include(p => p.Ministry)
                .Include(p => p.Donors)
                .Include(p => p.SuperVisor)
                .Include(p => p.ProjectManager)
                .Include(p => p.Governorates)
                .Include(p => p.Phases)
                    .ThenInclude(pp => pp.ActionPlan)
                        .ThenInclude(ap => ap.Plans)
                .ToListAsync();
            var sectors = await _context.Sectors.Include(s => s.Projects).ToListAsync();
            var ministries = await _context.Ministries.Include(m => m.Projects).ToListAsync();
            var donors = await _context.Donors.Include(d => d.Projects).ToListAsync();
            var supervisors = await _context.SuperVisors.ToListAsync();
            var projectManagers = await _context.ProjectManagers.ToListAsync();
            var governorates = await _context.Governorates.Include(g => g.projects).ToListAsync();

            // Summary Counts
            viewModel.TotalFrameworks = frameworks.Count;
            viewModel.TotalOutcomes = frameworks.SelectMany(f => f.Outcomes).Count();
            viewModel.TotalOutputs = frameworks.SelectMany(f => f.Outcomes).SelectMany(o => o.Outputs).Count();
            viewModel.TotalSubOutputs = frameworks.SelectMany(f => f.Outcomes).SelectMany(o => o.Outputs).SelectMany(op => op.SubOutputs).Count();
            viewModel.TotalIndicators = frameworks.SelectMany(f => f.Outcomes).SelectMany(o => o.Outputs).SelectMany(op => op.SubOutputs).SelectMany(so => so.Indicators).Count();
            viewModel.TotalProjects = projects.Count;

            // Average Performance
            viewModel.AverageIndicatorsPerformance = frameworks.Any() ? Math.Round(frameworks.Average(f => f.IndicatorsPerformance), 2) : 0;
            viewModel.AverageDisbursementPerformance = frameworks.Any() ? Math.Round(frameworks.Average(f => f.DisbursementPerformance), 2) : 0;

            // Framework Performance Data (for charts)
            viewModel.FrameworkPerformanceData = frameworks.Select(f => new PerformanceDataItem
            {
                Name = f.Name,
                Code = f.Code,
                IndicatorsPerformance = Math.Round(f.IndicatorsPerformance, 2),
                DisbursementPerformance = Math.Round(f.DisbursementPerformance, 2)
            }).ToList();

            // Top 5 Performers at each level
            viewModel.TopFrameworks = frameworks
                .OrderByDescending(f => f.IndicatorsPerformance)
                .Take(5)
                .Select(f => new PerformanceDataItem
                {
                    Name = f.Name,
                    Code = f.Code,
                    IndicatorsPerformance = Math.Round(f.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(f.DisbursementPerformance, 2)
                }).ToList();

            viewModel.TopOutcomes = frameworks
                .SelectMany(f => f.Outcomes)
                .OrderByDescending(o => o.IndicatorsPerformance)
                .Take(5)
                .Select(o => new PerformanceDataItem
                {
                    Name = o.Name,
                    Code = o.Code,
                    IndicatorsPerformance = Math.Round(o.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(o.DisbursementPerformance, 2),
                    ParentName = o.Framework?.Name
                }).ToList();

            viewModel.TopOutputs = frameworks
                .SelectMany(f => f.Outcomes)
                .SelectMany(o => o.Outputs)
                .OrderByDescending(op => op.IndicatorsPerformance)
                .Take(5)
                .Select(op => new PerformanceDataItem
                {
                    Name = op.Name,
                    Code = op.Code,
                    IndicatorsPerformance = Math.Round(op.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(op.DisbursementPerformance, 2),
                    ParentName = op.Outcome?.Name
                }).ToList();

            viewModel.TopSubOutputs = frameworks
                .SelectMany(f => f.Outcomes)
                .SelectMany(o => o.Outputs)
                .SelectMany(op => op.SubOutputs)
                .OrderByDescending(so => so.IndicatorsPerformance)
                .Take(5)
                .Select(so => new PerformanceDataItem
                {
                    Name = so.Name,
                    Code = so.Code,
                    IndicatorsPerformance = Math.Round(so.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(so.DisbursementPerformance, 2),
                    ParentName = so.Output?.Name
                }).ToList();

            viewModel.TopIndicators = frameworks
                .SelectMany(f => f.Outcomes)
                .SelectMany(o => o.Outputs)
                .SelectMany(op => op.SubOutputs)
                .SelectMany(so => so.Indicators)
                .OrderByDescending(i => i.IndicatorsPerformance)
                .Take(5)
                .Select(i => new PerformanceDataItem
                {
                    Name = i.Name,
                    Code = i.IndicatorCode,
                    IndicatorsPerformance = Math.Round(i.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(i.DisbursementPerformance, 2),
                    ParentName = i.SubOutput?.Name
                }).ToList();

            // Bottom 5 Performers at each level
            viewModel.BottomFrameworks = frameworks
                .OrderBy(f => f.IndicatorsPerformance)
                .Take(5)
                .Select(f => new PerformanceDataItem
                {
                    Name = f.Name,
                    Code = f.Code,
                    IndicatorsPerformance = Math.Round(f.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(f.DisbursementPerformance, 2)
                }).ToList();

            viewModel.BottomOutcomes = frameworks
                .SelectMany(f => f.Outcomes)
                .OrderBy(o => o.IndicatorsPerformance)
                .Take(5)
                .Select(o => new PerformanceDataItem
                {
                    Name = o.Name,
                    Code = o.Code,
                    IndicatorsPerformance = Math.Round(o.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(o.DisbursementPerformance, 2),
                    ParentName = o.Framework?.Name
                }).ToList();

            viewModel.BottomOutputs = frameworks
                .SelectMany(f => f.Outcomes)
                .SelectMany(o => o.Outputs)
                .OrderBy(op => op.IndicatorsPerformance)
                .Take(5)
                .Select(op => new PerformanceDataItem
                {
                    Name = op.Name,
                    Code = op.Code,
                    IndicatorsPerformance = Math.Round(op.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(op.DisbursementPerformance, 2),
                    ParentName = op.Outcome?.Name
                }).ToList();

            viewModel.BottomSubOutputs = frameworks
                .SelectMany(f => f.Outcomes)
                .SelectMany(o => o.Outputs)
                .SelectMany(op => op.SubOutputs)
                .OrderBy(so => so.IndicatorsPerformance)
                .Take(5)
                .Select(so => new PerformanceDataItem
                {
                    Name = so.Name,
                    Code = so.Code,
                    IndicatorsPerformance = Math.Round(so.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(so.DisbursementPerformance, 2),
                    ParentName = so.Output?.Name
                }).ToList();

            viewModel.BottomIndicators = frameworks
                .SelectMany(f => f.Outcomes)
                .SelectMany(o => o.Outputs)
                .SelectMany(op => op.SubOutputs)
                .SelectMany(so => so.Indicators)
                .OrderBy(i => i.IndicatorsPerformance)
                .Take(5)
                .Select(i => new PerformanceDataItem
                {
                    Name = i.Name,
                    Code = i.IndicatorCode,
                    IndicatorsPerformance = Math.Round(i.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(i.DisbursementPerformance, 2),
                    ParentName = i.SubOutput?.Name
                }).ToList();

            // Performance Distribution (for pie/doughnut charts)
            var allOutcomes = frameworks.SelectMany(f => f.Outcomes).ToList();
            viewModel.HighPerformanceCount = allOutcomes.Count(o => o.IndicatorsPerformance >= 75);
            viewModel.MediumPerformanceCount = allOutcomes.Count(o => o.IndicatorsPerformance >= 50 && o.IndicatorsPerformance < 75);
            viewModel.LowPerformanceCount = allOutcomes.Count(o => o.IndicatorsPerformance < 50);

            // Populate New Chart Data
            // 1. Sector Performance
            viewModel.SectorPerformanceData = sectors.Select(s => new PerformanceDataItem
            {
                Name = CultureInfo.CurrentCulture.Name.StartsWith("ar") ? s.AR_Name : s.EN_Name,
                Code = s.Code,
                IndicatorsPerformance = Math.Round(s.IndicatorsPerformance, 2),
                DisbursementPerformance = Math.Round(s.DisbursementPerformance, 2)
            }).OrderByDescending(s => s.IndicatorsPerformance).ToList();

            // 2. Ministry Performance
            viewModel.MinistryPerformanceData = ministries.Select(m => new PerformanceDataItem
            {
                Name = CultureInfo.CurrentCulture.Name.StartsWith("ar") ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN,
                Code = m.Code,
                IndicatorsPerformance = Math.Round(m.IndicatorsPerformance, 2),
                DisbursementPerformance = Math.Round(m.DisbursementPerformance, 2)
            }).OrderByDescending(m => m.IndicatorsPerformance).ToList();

            // 3. Project Financial vs Physical (Scatter Plot)
            viewModel.ProjectScatterData = projects.Select(p => new ProjectScatterDataItem
            {
                ProjectName = p.ProjectName,
                FinancialProgress = p.DisbursementPerformance,
                PhysicalProgress = p.performance,
                Budget = p.RealBudget
            }).ToList();

            // 4. Budget Overview
            viewModel.BudgetOverview = new BudgetOverviewItem
            {
                TotalEstimatedBudget = projects.Sum(p => p.EstimatedBudget),
                TotalRealBudget = projects.Sum(p => p.RealBudget)
            };

            // NEW: Category Reports
            var isArabic = CultureInfo.CurrentCulture.Name.StartsWith("ar");

            // Ministry Reports
            viewModel.TotalMinistries = ministries.Count;
            viewModel.MinistryReports = ministries
                .Where(m => m.Projects.Any())
                .Select(m => {
                    var ministryProjects = projects.Where(p => p.MinistryCode == m.Code).ToList();
                    return new CategoryReportItem
                    {
                        Name = isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN,
                        NameAr = m.MinistryDisplayName_AR,
                        ProjectCount = m.Projects.Count,
                        TotalBudget = m.Projects.Sum(p => p.EstimatedBudget),
                        AmountSpent = ministryProjects.Sum(p => p.Phases?.Where(pp => pp.ActionPlan != null).SelectMany(pp => pp.ActionPlan!.Plans).Sum(plan => (double)plan.Realised) ?? 0),
                        IndicatorsPerformance = Math.Round(m.IndicatorsPerformance, 2),
                        DisbursementPerformance = Math.Round(m.DisbursementPerformance, 2)
                    };
                })
                .OrderByDescending(m => m.ProjectCount)
                .ToList();

            // Sector Reports
            viewModel.TotalSectors = sectors.Count;
            viewModel.SectorReports = sectors
                .Where(s => s.Projects.Any())
                .Select(s => {
                    var sectorProjectIds = s.Projects.Select(p => p.ProjectID).ToList();
                    var sectorProjects = projects.Where(p => sectorProjectIds.Contains(p.ProjectID)).ToList();
                    return new CategoryReportItem
                    {
                        Name = isArabic ? s.AR_Name : s.EN_Name,
                        NameAr = s.AR_Name,
                        ProjectCount = s.Projects.Count,
                        TotalBudget = s.Projects.Sum(p => p.EstimatedBudget),
                        AmountSpent = sectorProjects.Sum(p => p.Phases?.Where(pp => pp.ActionPlan != null).SelectMany(pp => pp.ActionPlan!.Plans).Sum(plan => (double)plan.Realised) ?? 0),
                        IndicatorsPerformance = Math.Round(s.IndicatorsPerformance, 2),
                        DisbursementPerformance = Math.Round(s.DisbursementPerformance, 2)
                    };
                })
                .OrderByDescending(s => s.ProjectCount)
                .ToList();

            // Donor Reports
            viewModel.TotalDonors = donors.Count;
            viewModel.DonorReports = donors
                .Where(d => d.Projects.Any())
                .Select(d => {
                    var donorProjectIds = d.Projects.Select(p => p.ProjectID).ToList();
                    var donorProjects = projects.Where(p => donorProjectIds.Contains(p.ProjectID)).ToList();
                    return new CategoryReportItem
                    {
                        Name = d.Partner,
                        ProjectCount = d.Projects.Count,
                        TotalBudget = d.Projects.Sum(p => p.EstimatedBudget),
                        AmountSpent = donorProjects.Sum(p => p.Phases?.Where(pp => pp.ActionPlan != null).SelectMany(pp => pp.ActionPlan!.Plans).Sum(plan => (double)plan.Realised) ?? 0),
                        IndicatorsPerformance = Math.Round(d.IndicatorsPerformance, 2),
                        DisbursementPerformance = Math.Round(d.DisbursementPerformance, 2)
                    };
                })
                .OrderByDescending(d => d.ProjectCount)
                .ToList();

            // Supervisor Reports
            viewModel.TotalSupervisors = supervisors.Count;
            viewModel.SupervisorReports = supervisors
                .Select(s => {
                    var supervisorProjects = projects.Where(p => p.SuperVisorCode == s.Code).ToList();
                    return new CategoryReportItem
                    {
                        Name = s.Name,
                        ProjectCount = supervisorProjects.Count,
                        TotalBudget = supervisorProjects.Sum(p => p.EstimatedBudget),
                        AmountSpent = supervisorProjects.Sum(p => p.Phases?.Where(pp => pp.ActionPlan != null).SelectMany(pp => pp.ActionPlan!.Plans).Sum(plan => (double)plan.Realised) ?? 0),
                        IndicatorsPerformance = supervisorProjects.Any() ? Math.Round(supervisorProjects.Average(p => p.performance), 2) : 0,
                        DisbursementPerformance = supervisorProjects.Any() ? Math.Round(supervisorProjects.Average(p => p.DisbursementPerformance), 2) : 0
                    };
                })
                .Where(s => s.ProjectCount > 0)
                .OrderByDescending(s => s.ProjectCount)
                .ToList();

            // Project Manager Reports
            viewModel.TotalProjectManagers = projectManagers.Count;
            viewModel.ProjectManagerReports = projectManagers
                .Select(pm => {
                    var pmProjects = projects.Where(p => p.ProjectManagerCode == pm.Code).ToList();
                    return new CategoryReportItem
                    {
                        Name = pm.Name,
                        ProjectCount = pmProjects.Count,
                        TotalBudget = pmProjects.Sum(p => p.EstimatedBudget),
                        AmountSpent = pmProjects.Sum(p => p.Phases?.Where(pp => pp.ActionPlan != null).SelectMany(pp => pp.ActionPlan!.Plans).Sum(plan => (double)plan.Realised) ?? 0),
                        IndicatorsPerformance = pmProjects.Any() ? Math.Round(pmProjects.Average(p => p.performance), 2) : 0,
                        DisbursementPerformance = pmProjects.Any() ? Math.Round(pmProjects.Average(p => p.DisbursementPerformance), 2) : 0
                    };
                })
                .Where(pm => pm.ProjectCount > 0)
                .OrderByDescending(pm => pm.ProjectCount)
                .ToList();

            // National projects (IsEntireCountry = true) count for every governorate
            var nationalProjects = projects.Where(p => p.IsEntireCountry).ToList();

            // Governorate Reports
            viewModel.TotalGovernorates = governorates.Count;
            viewModel.GovernorateReports = governorates
                .Select(g => {
                    var govProjectIds = g.projects.Select(p => p.ProjectID).ToList();
                    var govProjects = projects.Where(p => govProjectIds.Contains(p.ProjectID)).ToList();
                    // Merge with national projects (avoid duplicates)
                    var allGovProjects = govProjects.Concat(nationalProjects)
                        .DistinctBy(p => p.ProjectID).ToList();
                    if (!allGovProjects.Any()) return null;
                    return new CategoryReportItem
                    {
                        Name = isArabic ? g.AR_Name : g.EN_Name,
                        NameAr = g.AR_Name,
                        ProjectCount = allGovProjects.Count,
                        TotalBudget = allGovProjects.Sum(p => p.EstimatedBudget),
                        AmountSpent = allGovProjects.Sum(p => p.Phases?.Where(pp => pp.ActionPlan != null).SelectMany(pp => pp.ActionPlan!.Plans).Sum(plan => (double)plan.Realised) ?? 0),
                        IndicatorsPerformance = Math.Round(allGovProjects.Average(p => p.performance), 2),
                        DisbursementPerformance = Math.Round(allGovProjects.Average(p => p.DisbursementPerformance), 2)
                    };
                })
                .Where(g => g != null)
                .OrderByDescending(g => g!.ProjectCount)
                .ToList()!;

            // Add "Entire Country" entry at the top if any national projects exist
            if (nationalProjects.Any())
            {
                viewModel.GovernorateReports.Insert(0, new CategoryReportItem
                {
                    Name = isArabic ? "الدولة بأكملها" : "Entire Country",
                    NameAr = "الدولة بأكملها",
                    ProjectCount = nationalProjects.Count,
                    TotalBudget = nationalProjects.Sum(p => p.EstimatedBudget),
                    AmountSpent = nationalProjects.Sum(p => p.Phases?.Where(pp => pp.ActionPlan != null).SelectMany(pp => pp.ActionPlan!.Plans).Sum(plan => (double)plan.Realised) ?? 0),
                    IndicatorsPerformance = Math.Round(nationalProjects.Average(p => p.performance), 2),
                    DisbursementPerformance = Math.Round(nationalProjects.Average(p => p.DisbursementPerformance), 2)
                });
            }

            return View(viewModel);
        }

        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> FinancialAnalysis()
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var projectsQuery = _context.Projects.AsQueryable();
            if (!isAdmin)
            {
                projectsQuery = scopedMinistryCode is null
                    ? projectsQuery.Where(_ => false)
                    : projectsQuery.Where(p => p.MinistryCode == scopedMinistryCode);
            }

            var projects = await projectsQuery
                .Select(p => new ProjectFinancialItem
                {
                    ProjectID    = p.ProjectID,
                    ProjectName  = p.ProjectName,
                    MinistryName = p.Ministries.Select(m => isArabic
                        ? m.MinistryDisplayName_AR
                        : m.MinistryDisplayName_EN).FirstOrDefault() ?? "",
                    EstimatedBudget = p.EstimatedBudget,
                    RealBudget = p.Phases
                        .Where(ph => ph.ActionPlan != null)
                        .SelectMany(ph => ph.ActionPlan!.Plans)
                        .Sum(plan => (double)plan.Realised),
                    Currency = p.Currency ?? "USD"
                })
                .OrderByDescending(p => p.EstimatedBudget)
                .ToListAsync();

            var vm = new FinancialAnalysisViewModel
            {
                Projects             = projects,
                TotalEstimatedBudget = projects.Sum(p => p.EstimatedBudget),
                TotalRealBudget      = projects.Sum(p => p.RealBudget),
                AverageSpendingRate  = projects.Any(p => p.EstimatedBudget > 0)
                    ? Math.Round(projects.Where(p => p.EstimatedBudget > 0)
                        .Average(p => p.SpendingRate), 1)
                    : 0,
                UnderSpendingCount = projects.Count(p => p.Status == SpendingStatus.UnderSpending),
                OnTargetCount      = projects.Count(p => p.Status == SpendingStatus.OnTarget),
                OverBudgetCount    = projects.Count(p => p.Status == SpendingStatus.OverBudget),
                NotStartedCount    = projects.Count(p => p.Status == SpendingStatus.NotStarted),
            };

            return View(vm);
        }

        // Governorate Map report — Syria map with cascading Strategy/Ministry/Project filters.
        // Clicking a governorate lists its projects; all filtering is done client-side from the flat Projects list.
        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> GovernorateMap()
        {
            var isArabic = CultureInfo.CurrentCulture.Name.StartsWith("ar");

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var projectsQuery = _context.Projects.AsQueryable();
            var frameworksQuery = _context.Frameworks.AsQueryable();
            if (!isAdmin)
            {
                projectsQuery = scopedMinistryCode is null
                    ? projectsQuery.Where(_ => false)
                    : projectsQuery.Where(p => p.MinistryCode == scopedMinistryCode);
                frameworksQuery = scopedMinistryCode is null
                    ? frameworksQuery.Where(_ => false)
                    : frameworksQuery.Where(f => f.MinistryCode == scopedMinistryCode);
            }

            var projects = await projectsQuery
                .Include(p => p.Ministry)
                .Include(p => p.Governorates)
                .Include(p => p.Phases)
                    .ThenInclude(pp => pp.ActionPlan)
                        .ThenInclude(ap => ap.Plans)
                .ToListAsync();

            // Build projectId -> set of strategy (framework) codes via the indicators hierarchy.
            var frameworks = await frameworksQuery
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                .ToListAsync();

            var projectFrameworks = new Dictionary<int, HashSet<int>>();
            foreach (var f in frameworks)
            {
                var projectIds = f.Outcomes
                    .SelectMany(o => o.Outputs)
                    .SelectMany(op => op.SubOutputs)
                    .SelectMany(so => so.Indicators)
                    .Where(i => i.ProjectID.HasValue)
                    .Select(i => i.ProjectID!.Value)
                    .Distinct();
                foreach (var pid in projectIds)
                {
                    if (!projectFrameworks.TryGetValue(pid, out var set))
                    {
                        set = new HashSet<int>();
                        projectFrameworks[pid] = set;
                    }
                    set.Add(f.Code);
                }
            }

            var governorates = await _context.Governorates.ToListAsync();
            var ministries = await _context.Ministries.ToListAsync();

            var viewModel = new GovernorateMapViewModel
            {
                TotalProjects = projects.Count,
                Governorates = governorates
                    .Select(g => new GovernorateRef { Code = g.Code, NameEn = g.EN_Name, NameAr = g.AR_Name })
                    .ToList(),
                Strategies = frameworks
                    .Select(f => new StrategyRef { Code = f.Code, Name = f.Name })
                    .OrderBy(s => s.Name)
                    .ToList(),
                Ministries = ministries
                    .Select(m => new MinistryRef
                    {
                        Code = m.Code,
                        Name = isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN
                    })
                    .OrderBy(m => m.Name)
                    .ToList(),
                Projects = projects
                    .Select(p => new GeoProjectItem
                    {
                        ProjectID = p.ProjectID,
                        ProjectName = p.ProjectName,
                        MinistryCode = p.MinistryCode,
                        Ministry = p.Ministry == null
                            ? string.Empty
                            : (isArabic ? p.Ministry.MinistryDisplayName_AR : p.Ministry.MinistryDisplayName_EN),
                        EstimatedBudget = p.EstimatedBudget,
                        Currency = p.Currency ?? "USD",
                        Performance = Math.Round(p.performance, 2),
                        DisbursementPerformance = Math.Round(p.DisbursementPerformance, 2),
                        TotalRealised = p.Phases
                            .Where(pp => pp.ActionPlan != null)
                            .SelectMany(pp => pp.ActionPlan!.Plans)
                            .Sum(pl => (double)pl.Realised),
                        StartDate = p.StartDate.ToString("yyyy-MM-dd"),
                        EndDate = p.EndDate.ToString("yyyy-MM-dd"),
                        IsNational = p.IsEntireCountry,
                        FrameworkCodes = projectFrameworks.TryGetValue(p.ProjectID, out var fc)
                            ? fc.ToList()
                            : new List<int>(),
                        GovernorateCodes = p.Governorates.Select(g => g.Code).ToList()
                    })
                    .ToList()
            };

            return View(viewModel);
        }
    }
}
