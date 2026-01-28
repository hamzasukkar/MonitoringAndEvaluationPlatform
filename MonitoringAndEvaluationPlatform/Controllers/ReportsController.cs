using Microsoft.AspNetCore.Authorization;
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

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Permission(Permissions.ViewControlPanel)]
        public async Task<IActionResult> Index()
        {
            var viewModel = new ReportsDashboardViewModel();

            // Get all data with includes
            var frameworks = await _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.ProjectIndicators)
                                    .ThenInclude(pi => pi.Project)
                .ToListAsync();

            var projects = await _context.Projects.Include(p => p.Sectors).Include(p => p.Ministry).ToListAsync();
            var sectors = await _context.Sectors.Include(s => s.Projects).ToListAsync();
            var ministries = await _context.Ministries.Include(m => m.Projects).ToListAsync();

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
                FinancialProgress = p.Financial, // Assuming 'Financial' is percentage (0-100)
                PhysicalProgress = p.Physical,   // Assuming 'Physical' is percentage (0-100)
                Budget = p.RealBudget
            }).ToList();

            // 4. Budget Overview
            viewModel.BudgetOverview = new BudgetOverviewItem
            {
                TotalEstimatedBudget = projects.Sum(p => p.EstimatedBudget),
                TotalRealBudget = projects.Sum(p => p.RealBudget)
            };

            return View(viewModel);
        }
    }
}
