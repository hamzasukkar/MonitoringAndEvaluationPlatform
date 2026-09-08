using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrencyConversionService _currencyConversion;
        private readonly IMinistryStatisticsService _ministryStatistics;
        private readonly IWebHostEnvironment _env;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ICurrencyConversionService currencyConversion, IMinistryStatisticsService ministryStatistics, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _currencyConversion = currencyConversion;
            _ministryStatistics = ministryStatistics;
            _env = env;
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
                .Include(p => p.Sector)
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

        // Units report — everything measured in a unit of measurement, grouped by that unit and
        // broken down by project and project date range.
        //
        // Two tracks feed it: impact indicators (target vs. summed yearly values) and phase
        // measures (phase target quantity vs. the latest measure recorded for the phase). Measure
        // quantities are cumulative-to-date rather than increments — see MeasuresController, which
        // derives Value as Quantity ÷ TargetQuantity — so summing every measure in a phase would
        // count the same delivery repeatedly. Only the newest one per phase is taken.
        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> UnitsReport(
            int? unitCode, int? ministryCode, int? projectId, DateTime? fromDate, DateTime? toDate)
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            var filter = new UnitsReportFilterViewModel
            {
                UnitCode     = unitCode,
                MinistryCode = ministryCode,
                ProjectId    = projectId,
                FromDate     = fromDate,
                ToDate       = toDate
            };

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();

            // Ministry scope first, then the user's own filters, so a MinistriesUser can never
            // widen past their own ministry by passing a ministryCode in the query string.
            var projectsQuery = _context.Projects.AsQueryable();
            if (!isAdmin)
            {
                projectsQuery = scopedMinistryCode is null
                    ? projectsQuery.Where(_ => false)
                    : projectsQuery.Where(p => p.MinistryCode == scopedMinistryCode);
            }
            else if (filter.MinistryCode is int chosenMinistry)
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.MinistryCode == chosenMinistry || p.Ministries.Any(m => m.Code == chosenMinistry));
            }

            if (filter.ProjectId is int chosenProject)
            {
                projectsQuery = projectsQuery.Where(p => p.ProjectID == chosenProject);
            }

            // Overlap, not containment: a project counts as being "in the period" if it was
            // running at any point during it. A 2023–2027 project appears for a 2025 range.
            if (filter.FromDate is DateTime from)
            {
                projectsQuery = projectsQuery.Where(p => p.EndDate >= from);
            }
            if (filter.ToDate is DateTime to)
            {
                projectsQuery = projectsQuery.Where(p => p.StartDate <= to);
            }

            var projectIds = await projectsQuery.Select(p => p.ProjectID).ToListAsync();

            // MeasurementUnit.DisplayName is [NotMapped] and culture-dependent, so unit names are
            // resolved here rather than inside a query EF would have to translate.
            var units = await _context.MeasurementUnits.AsNoTracking().ToListAsync();
            var unitNames = units.ToDictionary(u => u.Code, u => u.DisplayName);

            var unspecifiedLabel = isArabic ? "بدون وحدة" : "Unspecified unit";
            string UnitName(int key) =>
                unitNames.TryGetValue(key, out var name) ? name : unspecifiedLabel;
            int UnitKey(int? code) =>
                code is int c && unitNames.ContainsKey(c) ? c : UnitReportGroup.UnspecifiedUnitKey;

            bool UnitMatches(int? code) =>
                filter.UnitCode is not int wanted || UnitKey(code) == wanted;

            // ── Track 1: impact indicators ──
            var impactRows = await _context.ImpactIndicators
                .AsNoTracking()
                .Where(i => projectIds.Contains(i.ProjectID))
                .Select(i => new
                {
                    i.UnitCode,
                    i.ProjectID,
                    i.Project.ProjectName,
                    MinistryName = (i.Project.Ministry != null
                        ? (isArabic
                            ? i.Project.Ministry.MinistryDisplayName_AR
                            : i.Project.Ministry.MinistryDisplayName_EN)
                        : i.Project.Ministries.Select(m => isArabic
                            ? m.MinistryDisplayName_AR
                            : m.MinistryDisplayName_EN).FirstOrDefault()) ?? "",
                    ItemName = i.Name,
                    Target   = i.TargetValue,
                    Achieved = i.YearlyValues.Sum(v => v.Value),
                    i.Project.StartDate,
                    i.Project.EndDate
                })
                .ToListAsync();

            var impactGroups = impactRows
                .Where(r => UnitMatches(r.UnitCode))
                .GroupBy(r => UnitKey(r.UnitCode))
                .Select(g => new UnitReportGroup
                {
                    UnitKey  = g.Key,
                    UnitName = UnitName(g.Key),
                    Rows = g.Select(r => new UnitReportRow
                    {
                        ProjectId    = r.ProjectID,
                        ProjectName  = r.ProjectName,
                        MinistryName = r.MinistryName,
                        ItemName     = r.ItemName,
                        Target       = r.Target,
                        Achieved     = r.Achieved,
                        ProjectStart = r.StartDate,
                        ProjectEnd   = r.EndDate
                    })
                    .OrderByDescending(r => r.Target)
                    .ToList()
                })
                .ToList();

            // ── Track 2: phase measures, one row per phase built from its newest measure ──
            var measureRows = await _context.Measures
                .AsNoTracking()
                .Where(m => m.Quantity != null && projectIds.Contains(m.ProjectPhase.ProjectID))
                .Select(m => new
                {
                    m.UnitCode,
                    m.ProjectPhase.ProjectID,
                    PhaseId   = m.ProjectPhaseId,
                    PhaseName = m.ProjectPhase.Name,
                    m.ProjectPhase.Project.ProjectName,
                    MinistryName = (m.ProjectPhase.Project.Ministry != null
                        ? (isArabic
                            ? m.ProjectPhase.Project.Ministry.MinistryDisplayName_AR
                            : m.ProjectPhase.Project.Ministry.MinistryDisplayName_EN)
                        : m.ProjectPhase.Project.Ministries.Select(mi => isArabic
                            ? mi.MinistryDisplayName_AR
                            : mi.MinistryDisplayName_EN).FirstOrDefault()) ?? "",
                    Target      = m.ProjectPhase.TargetQuantity,
                    Quantity    = m.Quantity,
                    MeasureDate = m.Date,
                    m.Code,
                    m.ProjectPhase.Project.StartDate,
                    m.ProjectPhase.Project.EndDate
                })
                .ToListAsync();

            var latestPerPhase = measureRows
                // Code breaks ties so two measures saved on the same date resolve to the one
                // entered last rather than to whichever the sort happened to surface.
                .GroupBy(m => m.PhaseId)
                .Select(g => g.OrderByDescending(m => m.MeasureDate).ThenByDescending(m => m.Code).First())
                .ToList();

            var measureGroups = latestPerPhase
                .Where(r => UnitMatches(r.UnitCode))
                .GroupBy(r => UnitKey(r.UnitCode))
                .Select(g => new UnitReportGroup
                {
                    UnitKey  = g.Key,
                    UnitName = UnitName(g.Key),
                    Rows = g.Select(r => new UnitReportRow
                    {
                        ProjectId    = r.ProjectID,
                        ProjectName  = r.ProjectName,
                        MinistryName = r.MinistryName,
                        ItemName     = r.PhaseName,
                        Target       = r.Target ?? 0,
                        Achieved     = r.Quantity ?? 0,
                        ProjectStart = r.StartDate,
                        ProjectEnd   = r.EndDate,
                        LastRecorded = r.MeasureDate
                    })
                    .OrderByDescending(r => r.Achieved)
                    .ToList()
                })
                .ToList();

            // Unspecified sorts last wherever it appears; the rest go by size, so the units the
            // portfolio actually leans on sit at the top.
            static List<UnitReportGroup> Ordered(IEnumerable<UnitReportGroup> groups) => groups
                .OrderBy(g => g.IsUnspecified)
                .ThenByDescending(g => g.Rows.Count)
                .ThenBy(g => g.UnitName)
                .ToList();

            var viewModel = new UnitsReportViewModel
            {
                Filter        = filter,
                ImpactGroups  = Ordered(impactGroups),
                MeasureGroups = Ordered(measureGroups)
            };

            // ── Filter option lists ──
            viewModel.UnitOptions = units
                .OrderBy(u => u.DisplayName)
                .Select(u => new SelectListItem(u.DisplayName, u.Code.ToString(), u.Code == filter.UnitCode))
                .ToList();
            viewModel.UnitOptions.Add(new SelectListItem(
                unspecifiedLabel,
                UnitReportGroup.UnspecifiedUnitKey.ToString(),
                filter.UnitCode == UnitReportGroup.UnspecifiedUnitKey));

            // Only offered to admins; a scoped user has exactly one ministry and the control is hidden.
            viewModel.MinistryOptions = isAdmin
                ? await _context.Ministries
                    .AsNoTracking()
                    .OrderBy(m => isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN)
                    .Select(m => new SelectListItem(
                        isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN,
                        m.Code.ToString(),
                        m.Code == filter.MinistryCode))
                    .ToListAsync()
                : new List<SelectListItem>();

            // Built from the ministry/date scope but NOT from the project filter itself, so the
            // dropdown still lists the alternatives once a project has been picked.
            var projectOptionsQuery = _context.Projects.AsQueryable();
            if (!isAdmin)
            {
                projectOptionsQuery = scopedMinistryCode is null
                    ? projectOptionsQuery.Where(_ => false)
                    : projectOptionsQuery.Where(p => p.MinistryCode == scopedMinistryCode);
            }
            else if (filter.MinistryCode is int optionMinistry)
            {
                projectOptionsQuery = projectOptionsQuery.Where(p =>
                    p.MinistryCode == optionMinistry || p.Ministries.Any(m => m.Code == optionMinistry));
            }
            if (filter.FromDate is DateTime optionFrom)
            {
                projectOptionsQuery = projectOptionsQuery.Where(p => p.EndDate >= optionFrom);
            }
            if (filter.ToDate is DateTime optionTo)
            {
                projectOptionsQuery = projectOptionsQuery.Where(p => p.StartDate <= optionTo);
            }

            viewModel.ProjectOptions = await projectOptionsQuery
                .AsNoTracking()
                .OrderBy(p => p.ProjectName)
                .Select(p => new SelectListItem(p.ProjectName, p.ProjectID.ToString(), p.ProjectID == filter.ProjectId))
                .ToListAsync();

            return View(viewModel);
        }

        // ─────────────────────────── Ministry report ───────────────────────────
        // Ministry became a level of the hierarchy (Ministry → Strategy → … → Project), so it
        // gets its own scorecard. Every figure comes from IMinistryStatisticsService, which the
        // dashboard's ministry tier also reads — the two surfaces cannot disagree.

        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> MinistryReport(int? ministryCode, DateTime? fromDate, DateTime? toDate)
        {
            return View(await BuildMinistryReportAsync(ministryCode, fromDate, toDate));
        }

        /// <summary>
        /// Builds the ministry report. Shared by the page and both exports so a downloaded file
        /// can never disagree with the screen it was downloaded from.
        /// </summary>
        private async Task<MinistryReportViewModel> BuildMinistryReportAsync(
            int? ministryCode, DateTime? fromDate, DateTime? toDate)
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            var filter = new MinistryReportFilterViewModel
            {
                MinistryCode = ministryCode,
                FromDate = fromDate,
                ToDate = toDate
            };

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();

            // Scope first, then the user's own filter: a MinistriesUser passing someone else's
            // ministryCode in the query string must still get only their own.
            if (!isAdmin && scopedMinistryCode is null)
            {
                return new MinistryReportViewModel { Filter = filter };
            }

            var effectiveMinistryCode = isAdmin ? filter.MinistryCode : scopedMinistryCode;

            var stats = await _ministryStatistics.GetAsync(
                effectiveMinistryCode, filter.FromDate, filter.ToDate);

            var converter = await _currencyConversion.GetConverterAsync();

            var projectsByMinistry = await _ministryStatistics.GetProjectsByMinistryAsync(
                stats.Select(m => m.Code), filter.FromDate, filter.ToDate);

            var groups = new List<MinistryReportGroup>();
            foreach (var ministry in stats)
            {
                var projects = projectsByMinistry.TryGetValue(ministry.Code, out var found)
                    ? found
                    : (IReadOnlyList<Project>)Array.Empty<Project>();

                groups.Add(new MinistryReportGroup
                {
                    Stats = ministry,
                    Rows = projects.Select(p =>
                    {
                        var factor = converter.FactorFor(p.Currency, p.ExchangeRate);
                        return new MinistryReportRow
                        {
                            ProjectId = p.ProjectID,
                            ProjectName = p.ProjectName,
                            SectorName = p.Sector == null
                                ? string.Empty
                                : (isArabic ? p.Sector.AR_Name : p.Sector.EN_Name) ?? string.Empty,
                            // Null rather than 0 when the currency cannot be converted, so the
                            // view shows a gap instead of an amount that reads as real.
                            BudgetSyp = factor is null ? null : p.EstimatedBudget * factor.Value,
                            DisbursedSyp = factor is null
                                ? null
                                : MinistryStatisticsService.RealisedOf(p) * factor.Value,
                            Performance = Math.Round(p.performance, 2),
                            DisbursementPerformance = Math.Round(p.DisbursementPerformance, 2),
                            StartDate = p.StartDate,
                            EndDate = p.EndDate,
                            LinkageMismatch = (p.MinistryCode == ministry.Code)
                                != p.Ministries.Any(m => m.Code == ministry.Code)
                        };
                    }).ToList()
                });
            }

            var viewModel = new MinistryReportViewModel
            {
                Filter = filter,
                Groups = groups
                    .OrderByDescending(g => g.Stats.ProjectAverageIndicators)
                    .ThenBy(g => g.Stats.DisplayName(isArabic))
                    .ToList(),
                // Only offered to admins; a scoped user has exactly one ministry and the control
                // is hidden, matching the Units report.
                MinistryOptions = isAdmin
                    ? await _context.Ministries
                        .AsNoTracking()
                        .OrderBy(m => isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN)
                        .Select(m => new SelectListItem(
                            isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN,
                            m.Code.ToString(),
                            m.Code == filter.MinistryCode))
                        .ToListAsync()
                    : new List<SelectListItem>()
            };

            return viewModel;
        }

        // Qualified export names: ReportsController had no server-side exports, and a bare
        // ExportExcel would collide the moment a second report needs one.
        [HttpGet]
        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> ExportMinistryReportExcel(int? ministryCode, DateTime? fromDate, DateTime? toDate)
        {
            var model = await BuildMinistryReportAsync(ministryCode, fromDate, toDate);
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(isArabic ? "الوزارات" : "Ministries");

            if (isArabic)
            {
                worksheet.RightToLeft = true;
            }

            string[] headers =
            {
                isArabic ? "الوزارة" : "Ministry",
                isArabic ? "متوسط المشاريع (%)" : "Project Average (%)",
                isArabic ? "تجميع الأهداف الاستراتيجية (%)" : "Strategy Roll-up (%)",
                isArabic ? "الإنفاق (%)" : "Disbursement (%)",
                isArabic ? "الموازنة (ل.س)" : "Budget (SYP)",
                isArabic ? "المنصرف (ل.س)" : "Disbursed (SYP)",
                isArabic ? "نسبة الإنفاق (%)" : "Spend Rate (%)",
                isArabic ? "الأهداف الاستراتيجية" : "Strategies",
                isArabic ? "المؤشرات" : "Indicators",
                isArabic ? "المشاريع" : "Projects",
                isArabic ? "قيد التنفيذ" : "Active",
                isArabic ? "منجزة" : "Completed",
                isArabic ? "تحقيق الأثر (%)" : "Impact Achievement (%)",
                isArabic ? "مخرجات الأثر" : "Impact Outputs",
                isArabic ? "مشاريع غير محوَّلة" : "Unconverted Projects"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var row = 2;
            foreach (var group in model.Groups)
            {
                var stats = group.Stats;
                worksheet.Cell(row, 1).Value = stats.DisplayName(isArabic);
                worksheet.Cell(row, 2).Value = stats.ProjectAverageIndicators;
                // Blank, not zero: a ministry that owns no strategy has no roll-up to report.
                if (stats.StrategyRollupIndicators.HasValue)
                {
                    worksheet.Cell(row, 3).Value = stats.StrategyRollupIndicators.Value;
                }
                worksheet.Cell(row, 4).Value = stats.ProjectAverageDisbursement;
                worksheet.Cell(row, 5).Value = stats.Budget.Syp;
                worksheet.Cell(row, 6).Value = stats.Disbursed.Syp;
                worksheet.Cell(row, 7).Value = stats.SpendRate;
                worksheet.Cell(row, 8).Value = stats.StrategyCount;
                worksheet.Cell(row, 9).Value = stats.IndicatorCount;
                worksheet.Cell(row, 10).Value = stats.ProjectCount;
                worksheet.Cell(row, 11).Value = stats.ActiveProjectCount;
                worksheet.Cell(row, 12).Value = stats.CompletedProjectCount;
                if (stats.ImpactWeightedAchievement.HasValue)
                {
                    worksheet.Cell(row, 13).Value = stats.ImpactWeightedAchievement.Value;
                }
                worksheet.Cell(row, 14).Value = stats.ImpactOutputCount;
                worksheet.Cell(row, 15).Value = stats.Budget.UnconvertedCount;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            var dataRange = worksheet.Range(1, 1, Math.Max(row - 1, 1), headers.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var filePrefix = isArabic ? "تقرير_الوزارات" : "MinistryReport";
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet]
        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> ExportMinistryReportPdf(int? ministryCode, DateTime? fromDate, DateTime? toDate)
        {
            var model = await BuildMinistryReportAsync(ministryCode, fromDate, toDate);
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            string Pct(double? value) => value.HasValue ? $"{Math.Round(value.Value, 2)}%" : "—";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    if (isArabic)
                    {
                        page.ContentFromRightToLeft();
                    }

                    page.Header()
                        .PaddingBottom(10)
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Medium)
                        .Column(col =>
                        {
                            col.Item().Text(isArabic ? "تقرير الوزارات" : "Ministry Report")
                                .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Text($"{(isArabic ? "تاريخ الإصدار" : "Generated on")}: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);  // Ministry
                                columns.RelativeColumn(2);  // Project average
                                columns.RelativeColumn(2);  // Strategy roll-up
                                columns.RelativeColumn(2);  // Budget
                                columns.RelativeColumn(2);  // Disbursed
                                columns.RelativeColumn(2);  // Counts
                                columns.RelativeColumn(2);  // Impact
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6)
                                    .Text(isArabic ? "الوزارة" : "Ministry").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6)
                                    .Text(isArabic ? "متوسط المشاريع" : "Project avg.").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6)
                                    .Text(isArabic ? "تجميع الأهداف" : "Strategy roll-up").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6)
                                    .Text(isArabic ? "الموازنة (ل.س)" : "Budget (SYP)").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6)
                                    .Text(isArabic ? "المنصرف (ل.س)" : "Disbursed (SYP)").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6)
                                    .Text(isArabic ? "أهداف/مؤشرات/مشاريع" : "Strat./Ind./Proj.").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6)
                                    .Text(isArabic ? "الأثر" : "Impact").FontColor(Colors.White).Bold();
                            });

                            foreach (var group in model.Groups)
                            {
                                var stats = group.Stats;

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text(stats.DisplayName(isArabic));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text(Pct(stats.ProjectAverageIndicators))
                                    .FontColor(stats.ProjectAverageIndicators >= 80 ? Colors.Green.Darken2
                                        : stats.ProjectAverageIndicators >= 50 ? Colors.Orange.Darken2
                                        : Colors.Red.Darken2);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text(Pct(stats.StrategyRollupIndicators));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text(stats.Budget.Syp.ToString("N0", CultureInfo.InvariantCulture)
                                          + (stats.Budget.IsComplete ? "" : " *"));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text(stats.Disbursed.Syp.ToString("N0", CultureInfo.InvariantCulture));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text($"{stats.StrategyCount} / {stats.IndicatorCount} / {stats.ProjectCount}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text($"{Pct(stats.ImpactWeightedAchievement)} ({stats.ImpactOutputCount})");
                            }
                        });

                    page.Footer()
                        .Column(col =>
                        {
                            // The asterisk on a budget marks a ministry with projects whose
                            // currency could not be converted — the same gap the screen flags.
                            if (model.Groups.Any(g => !g.Stats.Budget.IsComplete))
                            {
                                col.Item().Text(isArabic
                                        ? "* الموازنة لا تشمل مشاريع بعملة بلا سعر صرف."
                                        : "* Budget excludes projects in a currency with no exchange rate.")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                            }

                            col.Item().AlignCenter().Text(text =>
                            {
                                text.Span((isArabic ? "صفحة" : "Page") + " ");
                                text.CurrentPageNumber();
                                text.Span(" / ");
                                text.TotalPages();
                            });
                        });
                });
            });

            var filePrefix = isArabic ? "تقرير_الوزارات" : "MinistryReport";
            return File(
                document.GeneratePdf(),
                "application/pdf",
                $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        // Projects map report — one page serving BOTH admin levels, governorate (stylised SVG of
        // Syria) and district (Leaflet over ADM2 boundaries), with cascading Strategy/Ministry/
        // Project filters. Clicking an area lists its projects; all filtering is done client-side
        // from the flat Projects list, so switching level never round-trips to the server.
        //
        // These were two near-identical pages (Reports/GovernorateMap and Reports/DistrictMap)
        // that differed only in the renderer and the geo facet; DistrictMap now redirects here.
        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> GovernorateMap(string? level = null)
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
                .Include(p => p.SubDistricts)
                .Include(p => p.Communities)
                .Include(p => p.Phases)
                    .ThenInclude(pp => pp.ActionPlan)
                        .ThenInclude(ap => ap.Plans)
                // Four collection includes plus the phases chain cartesian-explode as one join;
                // split them into separate round trips instead.
                .AsSplitQuery()
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
            var subDistricts = await _context.SubDistricts.ToListAsync();
            var ministries = await _context.Ministries.ToListAsync();

            // SubDistrict only carries DistrictCode, but the cascading governorate -> district
            // filter needs a governorate on every sub-district. Resolved from the districts already
            // loaded above rather than an .Include(s => s.District), which would re-query per row.
            var governorateByDistrict = districts.ToDictionary(d => d.Code, d => d.GovernorateCode);

            var viewModel = new GovernorateMapViewModel
            {
                Level = MapLevels.Normalize(level),
                // Checked server-side so the sub-district level can explain itself without
                // firing a request that is known to 404.
                HasSubDistrictBoundaries = System.IO.File.Exists(
                    Path.Combine(_env.WebRootPath ?? string.Empty, "geo", "syr_admin3.json")),
                TotalProjects = projects.Count,
                Governorates = governorates
                    .Select(g => new GovernorateRef { Code = g.Code, NameEn = g.EN_Name, NameAr = g.AR_Name })
                    .ToList(),
                Districts = districts
                    .Select(d => new DistrictRef
                    {
                        Code = d.Code,
                        NameEn = d.EN_Name,
                        NameAr = d.AR_Name,
                        GovernorateCode = d.GovernorateCode
                    })
                    .ToList(),
                SubDistricts = subDistricts
                    .Select(s => new SubDistrictRef
                    {
                        Code = s.Code,
                        NameEn = s.EN_Name,
                        NameAr = s.AR_Name,
                        DistrictCode = s.DistrictCode,
                        GovernorateCode = governorateByDistrict.TryGetValue(s.DistrictCode, out var gc)
                            ? gc
                            : string.Empty
                    })
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
                        SubDistrictCodes = p.SubDistricts.Select(s => s.Code).ToList(),
                        Communities = p.Communities
                            .Select(c => isArabic ? c.AR_Name : c.EN_Name)
                            .ToList()
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        // The district map is no longer its own page: it is the district level of GovernorateMap.
        // Kept as a redirect so existing bookmarks, sidebar links and shared URLs still land on the
        // right view instead of 404-ing.
        [Permission(Permissions.ViewControlPanel)]
        public IActionResult DistrictMap() =>
            RedirectToAction(nameof(GovernorateMap), new { level = MapLevels.District });
    }
}
