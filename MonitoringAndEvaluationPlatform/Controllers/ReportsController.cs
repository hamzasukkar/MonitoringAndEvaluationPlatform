using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;
using System.Globalization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrencyConversionService _currencyConversion;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ICurrencyConversionService currencyConversion)
        {
            _context = context;
            _userManager = userManager;
            _currencyConversion = currencyConversion;
        }

        /// <summary>
        /// Total disbursed across projects, converted to SYP. A project whose currency cannot be
        /// converted contributes nothing here; the Exchange Rates admin screen lists those so the
        /// gap is discoverable rather than invisible.
        /// </summary>
        private static double SumRealisedInSyp(IEnumerable<Project> projects, CurrencyConverter converter)
        {
            double total = 0;
            foreach (var project in projects)
            {
                var factor = converter.FactorFor(project.Currency, project.ExchangeRate);
                if (factor is null) continue;

                var realised = project.Phases?
                    .Where(phase => phase.ActionPlan != null)
                    .SelectMany(phase => phase.ActionPlan!.Plans)
                    .Sum(plan => (double)plan.Realised) ?? 0;

                total += realised * factor.Value;
            }
            return total;
        }

        /// <summary>
        /// Builds one category row from a set of projects. Averages are guarded against an empty
        /// sequence because callers legitimately pass one — a governorate with no projects of its
        /// own still needs a zeroed provincial slice rather than an exception.
        /// </summary>
        private static CategoryReportItem BuildCategoryReport(IReadOnlyCollection<Project> projects, CurrencyConverter converter)
        {
            var budget = converter.SumBudget(projects);
            return new CategoryReportItem
            {
                ProjectCount = projects.Count,
                TotalBudget = budget.Syp,
                AmountSpent = SumRealisedInSyp(projects, converter),
                IndicatorsPerformance = projects.Count > 0
                    ? Math.Round(projects.Average(p => p.performance), 2)
                    : 0,
                DisbursementPerformance = projects.Count > 0
                    ? Math.Round(projects.Average(p => p.DisbursementPerformance), 2)
                    : 0,
                UnconvertedProjectCount = budget.UnconvertedCount
            };
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

            // Every figure below puts projects that may be denominated differently onto one
            // chart or into one sum, so all budgets are converted to SYP first.
            var conv = await _currencyConversion.GetConverterAsync();

            // 3. Project Financial vs Physical (Scatter Plot)
            viewModel.ProjectScatterData = projects.Select(p => new ProjectScatterDataItem
            {
                ProjectName = p.ProjectName,
                FinancialProgress = p.DisbursementPerformance,
                PhysicalProgress = p.performance,
                Budget = conv.ToSyp(p.RealBudget, p.Currency, p.ExchangeRate) ?? 0
            }).ToList();

            // 4. Budget Overview
            viewModel.BudgetOverview = new BudgetOverviewItem
            {
                TotalEstimatedBudget = conv.SumBudget(projects).Syp,
                TotalRealBudget = conv.SumRealBudget(projects).Syp
            };

            // NEW: Category Reports
            var isArabic = CultureInfo.CurrentCulture.Name.StartsWith("ar");

            // Ministry Reports
            //
            // Every aggregate reads the same set: the ministry's projects intersected with the
            // ministry-scoped `projects` list. The many-to-many navigation (m.Projects) is loaded
            // unscoped, so using it directly leaked other ministries' counts and budgets to a
            // MinistriesUser, and disagreed with AmountSpent, which was already scoped.
            viewModel.TotalMinistries = ministries.Count;
            viewModel.MinistryReports = ministries
                .Select(m => {
                    var ministryProjectIds = m.Projects.Select(p => p.ProjectID).ToList();
                    var ministryProjects = projects.Where(p => ministryProjectIds.Contains(p.ProjectID)).ToList();
                    var report = BuildCategoryReport(ministryProjects, conv);
                    report.Name = isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN;
                    report.NameAr = m.MinistryDisplayName_AR;
                    report.IndicatorsPerformance = Math.Round(m.IndicatorsPerformance, 2);
                    report.DisbursementPerformance = Math.Round(m.DisbursementPerformance, 2);
                    return report;
                })
                .Where(m => m.ProjectCount > 0)
                .OrderByDescending(m => m.ProjectCount)
                .ToList();

            // Sector Reports
            viewModel.TotalSectors = sectors.Count;
            viewModel.SectorReports = sectors
                .Select(s => {
                    var sectorProjectIds = s.Projects.Select(p => p.ProjectID).ToList();
                    var sectorProjects = projects.Where(p => sectorProjectIds.Contains(p.ProjectID)).ToList();
                    var report = BuildCategoryReport(sectorProjects, conv);
                    report.Name = isArabic ? s.AR_Name : s.EN_Name;
                    report.NameAr = s.AR_Name;
                    report.IndicatorsPerformance = Math.Round(s.IndicatorsPerformance, 2);
                    report.DisbursementPerformance = Math.Round(s.DisbursementPerformance, 2);
                    return report;
                })
                .Where(s => s.ProjectCount > 0)
                .OrderByDescending(s => s.ProjectCount)
                .ToList();

            // Public Sector Type Reports (counts use the scoped projects list so ministry scoping is respected)
            var publicSectorTypes = await _context.PublicSectorTypes.ToListAsync();
            viewModel.TotalPublicSectorTypes = publicSectorTypes.Count;
            viewModel.PublicSectorTypeReports = publicSectorTypes
                .Select(t => {
                    var typeProjects = projects.Where(p => p.PublicSectorTypeCode == t.Code).ToList();
                    return new CategoryReportItem
                    {
                        Name = isArabic ? t.AR_Name : t.EN_Name,
                        NameAr = t.AR_Name,
                        ProjectCount = typeProjects.Count,
                        TotalBudget = conv.SumBudget(typeProjects).Syp,
                        AmountSpent = SumRealisedInSyp(typeProjects, conv)
                    };
                })
                .Where(t => t.ProjectCount > 0)
                .OrderByDescending(t => t.ProjectCount)
                .ToList();

            // Donor Reports
            viewModel.TotalDonors = donors.Count;
            viewModel.DonorReports = donors
                .Select(d => {
                    var donorProjectIds = d.Projects.Select(p => p.ProjectID).ToList();
                    var donorProjects = projects.Where(p => donorProjectIds.Contains(p.ProjectID)).ToList();
                    var report = BuildCategoryReport(donorProjects, conv);
                    report.Name = d.Partner;
                    report.IndicatorsPerformance = Math.Round(d.IndicatorsPerformance, 2);
                    report.DisbursementPerformance = Math.Round(d.DisbursementPerformance, 2);
                    return report;
                })
                .Where(d => d.ProjectCount > 0)
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
                        TotalBudget = conv.SumBudget(supervisorProjects).Syp,
                        AmountSpent = SumRealisedInSyp(supervisorProjects, conv),
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
                        TotalBudget = conv.SumBudget(pmProjects).Syp,
                        AmountSpent = SumRealisedInSyp(pmProjects, conv),
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
            //
            // Each row carries TWO slices. The inherited figures are the merged view (this
            // governorate's own projects plus every nationwide one), which is what the map's "All"
            // level shows. Provincial carries the province-only slice.
            //
            // Both are computed here rather than derived in JavaScript: the map used to subtract the
            // national COUNT client-side while reading the merged BUDGET verbatim, which is why a
            // governorate with no projects of its own displayed 0 projects next to the entire
            // national budget.
            viewModel.TotalGovernorates = governorates.Count;
            viewModel.GovernorateReports = governorates
                .Select(g => {
                    var govProjectIds = g.projects.Select(p => p.ProjectID).ToList();
                    var govProjects = projects.Where(p => govProjectIds.Contains(p.ProjectID)).ToList();
                    // Merge with national projects (avoid duplicates)
                    var allGovProjects = govProjects.Concat(nationalProjects)
                        .DistinctBy(p => p.ProjectID).ToList();
                    if (!allGovProjects.Any()) return null;

                    var merged = BuildCategoryReport(allGovProjects, conv);
                    return new GovernorateReportItem
                    {
                        Name = isArabic ? g.AR_Name : g.EN_Name,
                        NameAr = g.AR_Name,
                        ProjectCount = merged.ProjectCount,
                        TotalBudget = merged.TotalBudget,
                        AmountSpent = merged.AmountSpent,
                        IndicatorsPerformance = merged.IndicatorsPerformance,
                        DisbursementPerformance = merged.DisbursementPerformance,
                        UnconvertedProjectCount = merged.UnconvertedProjectCount,
                        // Legitimately an all-zero slice when the governorate has no own projects.
                        Provincial = BuildCategoryReport(govProjects, conv)
                    };
                })
                .Where(g => g != null)
                .OrderByDescending(g => g!.ProjectCount)
                .ToList()!;

            // Add "Entire Country" entry at the top if any national projects exist
            if (nationalProjects.Any())
            {
                var national = BuildCategoryReport(nationalProjects, conv);
                viewModel.GovernorateReports.Insert(0, new GovernorateReportItem
                {
                    Name = isArabic ? "الدولة بأكملها" : "Entire Country",
                    NameAr = "الدولة بأكملها",
                    ProjectCount = national.ProjectCount,
                    TotalBudget = national.TotalBudget,
                    AmountSpent = national.AmountSpent,
                    IndicatorsPerformance = national.IndicatorsPerformance,
                    DisbursementPerformance = national.DisbursementPerformance,
                    UnconvertedProjectCount = national.UnconvertedProjectCount,
                    // "Entire Country" has no provincial slice by definition.
                    Provincial = null
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
                    Currency = p.Currency ?? "USD",
                    ExchangeRate = p.ExchangeRate
                })
                .OrderByDescending(p => p.EstimatedBudget)
                .ToListAsync();

            // Per-project figures stay in each project's own currency; only the totals below
            // are converted, since they combine projects that may be denominated differently.
            var conv = await _currencyConversion.GetConverterAsync();
            double SumInSyp(Func<ProjectFinancialItem, double> selector) => projects
                .Sum(p => (conv.ToSyp(selector(p), p.Currency, p.ExchangeRate)) ?? 0);

            var vm = new FinancialAnalysisViewModel
            {
                Projects             = projects,
                TotalEstimatedBudget = SumInSyp(p => p.EstimatedBudget),
                TotalRealBudget      = SumInSyp(p => p.RealBudget),
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
                .Include(p => p.Communities)
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
                        GovernorateCodes = p.Governorates.Select(g => g.Code).ToList(),
                        Communities = p.Communities
                            .Select(c => isArabic ? c.AR_Name : c.EN_Name)
                            .ToList()
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        // District Map report — Leaflet map of Syria's districts (ADM2) with the same cascading
        // Strategy/Ministry/Project filters as GovernorateMap. Districts are joined to the GeoJSON
        // boundaries (wwwroot/geo/syr_admin2.json) by PCode (District.Code).
        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> DistrictMap()
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
                .Include(p => p.Districts)
                .Include(p => p.Communities)
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
            var districts = await _context.Districts.ToListAsync();
            var ministries = await _context.Ministries.ToListAsync();

            var viewModel = new DistrictMapViewModel
            {
                TotalProjects = projects.Count,
                Districts = districts
                    .Select(d => new DistrictRef
                    {
                        Code = d.Code,
                        NameEn = d.EN_Name,
                        NameAr = d.AR_Name,
                        GovernorateCode = d.GovernorateCode
                    })
                    .ToList(),
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
                        GovernorateCodes = p.Governorates.Select(g => g.Code).ToList(),
                        DistrictCodes = p.Districts.Select(d => d.Code).ToList(),
                        Communities = p.Communities
                            .Select(c => isArabic ? c.AR_Name : c.EN_Name)
                            .ToList()
                    })
                    .ToList()
            };

            return View(viewModel);
        }
    }
}
