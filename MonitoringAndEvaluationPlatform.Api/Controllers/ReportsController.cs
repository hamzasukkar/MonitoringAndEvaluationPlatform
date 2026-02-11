using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Dtos;

namespace MonitoringAndEvaluationPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<ReportsSummaryDto>> GetSummary()
        {
            var frameworks = await _context.Frameworks.ToListAsync();
            var projects = await _context.Projects.ToListAsync();
            var sectors = await _context.Sectors.ToListAsync();
            var ministries = await _context.Ministries.ToListAsync();

            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            var dto = new ReportsSummaryDto
            {
                TotalFrameworks = frameworks.Count,
                TotalOutcomes = await _context.Outcomes.CountAsync(),
                TotalOutputs = await _context.Outputs.CountAsync(),
                TotalSubOutputs = await _context.SubOutputs.CountAsync(),
                TotalIndicators = await _context.Indicators.CountAsync(),
                TotalProjects = projects.Count,
                AverageIndicatorsPerformance = frameworks.Any() ? Math.Round(frameworks.Average(f => f.IndicatorsPerformance), 2) : 0,
                AverageDisbursementPerformance = frameworks.Any() ? Math.Round(frameworks.Average(f => f.DisbursementPerformance), 2) : 0,
                FrameworkPerformanceData = frameworks.Select(f => new PerformanceItemDto
                {
                    Name = f.Name,
                    Code = f.Code,
                    IndicatorsPerformance = Math.Round(f.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(f.DisbursementPerformance, 2)
                }).ToList(),
                SectorPerformanceData = sectors.Select(s => new PerformanceItemDto
                {
                    Name = isArabic ? s.AR_Name : s.EN_Name,
                    Code = s.Code,
                    IndicatorsPerformance = Math.Round(s.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(s.DisbursementPerformance, 2)
                }).OrderByDescending(s => s.IndicatorsPerformance).ToList(),
                MinistryPerformanceData = ministries.Select(m => new PerformanceItemDto
                {
                    Name = isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN,
                    Code = m.Code,
                    IndicatorsPerformance = Math.Round(m.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(m.DisbursementPerformance, 2)
                }).OrderByDescending(m => m.IndicatorsPerformance).ToList(),
                BudgetOverview = new BudgetOverviewDto
                {
                    TotalEstimatedBudget = projects.Sum(p => p.EstimatedBudget),
                    TotalRealBudget = projects.Sum(p => p.RealBudget)
                },
                PerformanceDistribution = new PerformanceDistributionDto()
            };

            var allOutcomes = await _context.Outcomes.ToListAsync();
            dto.PerformanceDistribution.HighPerformanceCount = allOutcomes.Count(o => o.IndicatorsPerformance >= 75);
            dto.PerformanceDistribution.MediumPerformanceCount = allOutcomes.Count(o => o.IndicatorsPerformance >= 50 && o.IndicatorsPerformance < 75);
            dto.PerformanceDistribution.LowPerformanceCount = allOutcomes.Count(o => o.IndicatorsPerformance < 50);

            return Ok(dto);
        }

        [HttpGet("top-performers")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<TopBottomPerformersDto>> GetTopPerformers([FromQuery] int count = 5)
        {
            var frameworks = await _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                .ToListAsync();

            var dto = new TopBottomPerformersDto
            {
                TopFrameworks = frameworks.OrderByDescending(f => f.IndicatorsPerformance).Take(count)
                    .Select(f => new PerformanceItemDto { Name = f.Name, Code = f.Code, IndicatorsPerformance = Math.Round(f.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(f.DisbursementPerformance, 2) }).ToList(),
                TopOutcomes = frameworks.SelectMany(f => f.Outcomes).OrderByDescending(o => o.IndicatorsPerformance).Take(count)
                    .Select(o => new PerformanceItemDto { Name = o.Name, Code = o.Code, IndicatorsPerformance = Math.Round(o.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(o.DisbursementPerformance, 2), ParentName = o.Framework?.Name }).ToList(),
                TopOutputs = frameworks.SelectMany(f => f.Outcomes).SelectMany(o => o.Outputs).OrderByDescending(op => op.IndicatorsPerformance).Take(count)
                    .Select(op => new PerformanceItemDto { Name = op.Name, Code = op.Code, IndicatorsPerformance = Math.Round(op.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(op.DisbursementPerformance, 2), ParentName = op.Outcome?.Name }).ToList(),
                TopSubOutputs = frameworks.SelectMany(f => f.Outcomes).SelectMany(o => o.Outputs).SelectMany(op => op.SubOutputs).OrderByDescending(so => so.IndicatorsPerformance).Take(count)
                    .Select(so => new PerformanceItemDto { Name = so.Name, Code = so.Code, IndicatorsPerformance = Math.Round(so.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(so.DisbursementPerformance, 2), ParentName = so.Output?.Name }).ToList(),
                TopIndicators = frameworks.SelectMany(f => f.Outcomes).SelectMany(o => o.Outputs).SelectMany(op => op.SubOutputs).SelectMany(so => so.Indicators).OrderByDescending(i => i.IndicatorsPerformance).Take(count)
                    .Select(i => new PerformanceItemDto { Name = i.Name, Code = i.IndicatorCode, IndicatorsPerformance = Math.Round(i.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(i.DisbursementPerformance, 2), ParentName = i.SubOutput?.Name }).ToList()
            };

            return Ok(dto);
        }

        [HttpGet("bottom-performers")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<TopBottomPerformersDto>> GetBottomPerformers([FromQuery] int count = 5)
        {
            var frameworks = await _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                .ToListAsync();

            var dto = new TopBottomPerformersDto
            {
                BottomFrameworks = frameworks.OrderBy(f => f.IndicatorsPerformance).Take(count)
                    .Select(f => new PerformanceItemDto { Name = f.Name, Code = f.Code, IndicatorsPerformance = Math.Round(f.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(f.DisbursementPerformance, 2) }).ToList(),
                BottomOutcomes = frameworks.SelectMany(f => f.Outcomes).OrderBy(o => o.IndicatorsPerformance).Take(count)
                    .Select(o => new PerformanceItemDto { Name = o.Name, Code = o.Code, IndicatorsPerformance = Math.Round(o.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(o.DisbursementPerformance, 2), ParentName = o.Framework?.Name }).ToList(),
                BottomOutputs = frameworks.SelectMany(f => f.Outcomes).SelectMany(o => o.Outputs).OrderBy(op => op.IndicatorsPerformance).Take(count)
                    .Select(op => new PerformanceItemDto { Name = op.Name, Code = op.Code, IndicatorsPerformance = Math.Round(op.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(op.DisbursementPerformance, 2), ParentName = op.Outcome?.Name }).ToList(),
                BottomSubOutputs = frameworks.SelectMany(f => f.Outcomes).SelectMany(o => o.Outputs).SelectMany(op => op.SubOutputs).OrderBy(so => so.IndicatorsPerformance).Take(count)
                    .Select(so => new PerformanceItemDto { Name = so.Name, Code = so.Code, IndicatorsPerformance = Math.Round(so.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(so.DisbursementPerformance, 2), ParentName = so.Output?.Name }).ToList(),
                BottomIndicators = frameworks.SelectMany(f => f.Outcomes).SelectMany(o => o.Outputs).SelectMany(op => op.SubOutputs).SelectMany(so => so.Indicators).OrderBy(i => i.IndicatorsPerformance).Take(count)
                    .Select(i => new PerformanceItemDto { Name = i.Name, Code = i.IndicatorCode, IndicatorsPerformance = Math.Round(i.IndicatorsPerformance, 2), DisbursementPerformance = Math.Round(i.DisbursementPerformance, 2), ParentName = i.SubOutput?.Name }).ToList()
            };

            return Ok(dto);
        }

        [HttpGet("ministry-performance")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<List<CategoryReportDto>>> GetMinistryPerformance()
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            var ministries = await _context.Ministries.Include(m => m.Projects).ToListAsync();
            var projects = await _context.Projects
                .Include(p => p.ActionPlan).ThenInclude(ap => ap.Activities).ThenInclude(a => a.Plans)
                .ToListAsync();

            var result = ministries.Where(m => m.Projects.Any()).Select(m =>
            {
                var ministryProjects = projects.Where(p => p.MinistryCode == m.Code).ToList();
                return new CategoryReportDto
                {
                    Name = isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN,
                    NameAr = m.MinistryDisplayName_AR,
                    ProjectCount = m.Projects.Count,
                    TotalBudget = m.Projects.Sum(p => p.EstimatedBudget),
                    AmountSpent = ministryProjects.Sum(p => p.ActionPlan?.Activities?.SelectMany(a => a.Plans)?.Sum(plan => plan.Realised) ?? 0),
                    IndicatorsPerformance = Math.Round(m.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(m.DisbursementPerformance, 2),
                    FieldMonitoring = Math.Round(m.FieldMonitoring, 2),
                    ImpactAssessment = Math.Round(m.ImpactAssessment, 2)
                };
            }).OrderByDescending(m => m.ProjectCount).ToList();

            return Ok(result);
        }

        [HttpGet("sector-performance")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<List<CategoryReportDto>>> GetSectorPerformance()
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            var sectors = await _context.Sectors.Include(s => s.Projects).ToListAsync();
            var projects = await _context.Projects
                .Include(p => p.ActionPlan).ThenInclude(ap => ap.Activities).ThenInclude(a => a.Plans)
                .ToListAsync();

            var result = sectors.Where(s => s.Projects.Any()).Select(s =>
            {
                var sectorProjectIds = s.Projects.Select(p => p.ProjectID).ToList();
                var sectorProjects = projects.Where(p => sectorProjectIds.Contains(p.ProjectID)).ToList();
                return new CategoryReportDto
                {
                    Name = isArabic ? s.AR_Name : s.EN_Name,
                    NameAr = s.AR_Name,
                    ProjectCount = s.Projects.Count,
                    TotalBudget = s.Projects.Sum(p => p.EstimatedBudget),
                    AmountSpent = sectorProjects.Sum(p => p.ActionPlan?.Activities?.SelectMany(a => a.Plans)?.Sum(plan => plan.Realised) ?? 0),
                    IndicatorsPerformance = Math.Round(s.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(s.DisbursementPerformance, 2),
                    FieldMonitoring = Math.Round(s.FieldMonitoring, 2),
                    ImpactAssessment = Math.Round(s.ImpactAssessment, 2)
                };
            }).OrderByDescending(s => s.ProjectCount).ToList();

            return Ok(result);
        }

        [HttpGet("donor-performance")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<List<CategoryReportDto>>> GetDonorPerformance()
        {
            var donors = await _context.Donors.Include(d => d.Projects).ToListAsync();
            var projects = await _context.Projects
                .Include(p => p.ActionPlan).ThenInclude(ap => ap.Activities).ThenInclude(a => a.Plans)
                .ToListAsync();

            var result = donors.Where(d => d.Projects.Any()).Select(d =>
            {
                var donorProjectIds = d.Projects.Select(p => p.ProjectID).ToList();
                var donorProjects = projects.Where(p => donorProjectIds.Contains(p.ProjectID)).ToList();
                return new CategoryReportDto
                {
                    Name = d.Partner,
                    ProjectCount = d.Projects.Count,
                    TotalBudget = d.Projects.Sum(p => p.EstimatedBudget),
                    AmountSpent = donorProjects.Sum(p => p.ActionPlan?.Activities?.SelectMany(a => a.Plans)?.Sum(plan => plan.Realised) ?? 0),
                    IndicatorsPerformance = Math.Round(d.IndicatorsPerformance, 2),
                    DisbursementPerformance = Math.Round(d.DisbursementPerformance, 2),
                    FieldMonitoring = Math.Round(d.FieldMonitoring, 2),
                    ImpactAssessment = Math.Round(d.ImpactAssessment, 2)
                };
            }).OrderByDescending(d => d.ProjectCount).ToList();

            return Ok(result);
        }

        [HttpGet("budget-overview")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<BudgetOverviewDto>> GetBudgetOverview()
        {
            var projects = await _context.Projects.ToListAsync();
            return Ok(new BudgetOverviewDto
            {
                TotalEstimatedBudget = projects.Sum(p => p.EstimatedBudget),
                TotalRealBudget = projects.Sum(p => p.RealBudget)
            });
        }

        [HttpGet("performance-distribution")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<PerformanceDistributionDto>> GetPerformanceDistribution()
        {
            var outcomes = await _context.Outcomes.ToListAsync();
            return Ok(new PerformanceDistributionDto
            {
                HighPerformanceCount = outcomes.Count(o => o.IndicatorsPerformance >= 75),
                MediumPerformanceCount = outcomes.Count(o => o.IndicatorsPerformance >= 50 && o.IndicatorsPerformance < 75),
                LowPerformanceCount = outcomes.Count(o => o.IndicatorsPerformance < 50)
            });
        }
    }
}
