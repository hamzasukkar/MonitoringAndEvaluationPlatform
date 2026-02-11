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
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            var summary = new DashboardSummaryDto
            {
                TotalFrameworks = await _context.Frameworks.CountAsync(),
                TotalIndicators = await _context.Indicators.CountAsync(),
                TotalProjects = await _context.Projects.CountAsync(),
                TotalGovernorates = await _context.Governorates.CountAsync(),
                TotalMinistries = await _context.Ministries.CountAsync(),
                TotalOutcomes = await _context.Outcomes.CountAsync(),
                TotalOutputs = await _context.Outputs.CountAsync(),
                TotalSubOutputs = await _context.SubOutputs.CountAsync()
            };

            return Ok(summary);
        }

        [HttpGet("frameworks-performance")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<List<FrameworkPerformanceDto>>> GetFrameworksPerformance(
            [FromQuery] int? frameworkCode = null,
            [FromQuery] int? ministryCode = null)
        {
            var frameworksQuery = _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.ProjectIndicators)
                                    .ThenInclude(pi => pi.Project)
                                        .ThenInclude(p => p.Ministries)
                .AsQueryable();

            var frameworks = await frameworksQuery.ToListAsync();

            var result = frameworks
                .Where(fw => frameworkCode == null || fw.Code == frameworkCode)
                .Select(fw =>
                {
                    var projects = fw.Outcomes
                        .SelectMany(o => o.Outputs)
                        .SelectMany(op => op.SubOutputs)
                        .SelectMany(so => so.Indicators)
                        .SelectMany(i => i.ProjectIndicators)
                        .Where(pi => pi.Project != null &&
                                     (ministryCode == null || pi.Project.Ministries.Any(min => min.Code == ministryCode)))
                        .Select(pi => pi.Project)
                        .Distinct()
                        .ToList();

                    return new FrameworkPerformanceDto
                    {
                        Code = fw.Code,
                        Name = fw.Name,
                        IndicatorsPerformance = Math.Round(fw.IndicatorsPerformance, 2),
                        IndicatorCount = fw.Outcomes
                            .SelectMany(o => o.Outputs)
                            .SelectMany(op => op.SubOutputs)
                            .SelectMany(so => so.Indicators)
                            .Count(),
                        Projects = projects.Select(p => new ProjectPerformanceDto
                        {
                            ProjectID = p!.ProjectID,
                            ProjectName = p.ProjectName,
                            Performance = Math.Round(p.performance, 2)
                        }).ToList()
                    };
                })
                .OrderByDescending(f => f.IndicatorsPerformance)
                .ToList();

            return Ok(result);
        }

        [HttpGet("outcome-progress")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<List<OutcomeProgressDto>>> GetOutcomeProgress(
            [FromQuery] int? frameworkCode = null)
        {
            var outcomesQuery = _context.Outcomes
                .Include(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                            .ThenInclude(i => i.Measures)
                .AsQueryable();

            if (frameworkCode.HasValue)
                outcomesQuery = outcomesQuery.Where(o => o.FrameworkCode == frameworkCode.Value);

            var outcomes = await outcomesQuery.ToListAsync();

            var items = outcomes.Select(o =>
            {
                var indicators = o.Outputs
                    .SelectMany(op => op.SubOutputs)
                    .SelectMany(so => so.Indicators)
                    .ToList();

                var totalTarget = indicators.Sum(i => i.Target);
                var totalAchieved = indicators.SelectMany(i => i.Measures).Sum(m => m.Value);
                var achievementRate = totalTarget > 0 ? (totalAchieved / totalTarget) * 100 : 0;

                return new OutcomeProgressDto
                {
                    OutcomeName = o.Name,
                    TotalIndicators = indicators.Count,
                    TotalTarget = totalTarget,
                    TotalAchieved = totalAchieved,
                    AchievementRate = Math.Round(achievementRate, 2)
                };
            })
            .OrderByDescending(x => x.AchievementRate)
            .ToList();

            return Ok(items);
        }

        [HttpGet("project-progress")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<List<ProjectProgressDto>>> GetProjectProgress(
            [FromQuery] int? sectorId = null,
            [FromQuery] int? donorId = null)
        {
            var projectsQuery = _context.Projects
                .Include(p => p.ProjectIndicators)
                    .ThenInclude(pi => pi.Indicator)
                        .ThenInclude(i => i.Measures)
                .AsQueryable();

            if (sectorId.HasValue)
                projectsQuery = projectsQuery.Where(p => p.Sectors!.Any(s => s.Code == sectorId.Value));

            if (donorId.HasValue)
                projectsQuery = projectsQuery.Where(p => p.Donors!.Any(d => d.Code == donorId.Value));

            var projects = await projectsQuery.ToListAsync();

            var projectProgress = projects.Select(p =>
            {
                var allMeasures = p.ProjectIndicators.SelectMany(pi => pi.Indicator.Measures).ToList();
                var indicators = p.ProjectIndicators.Select(pi => pi.Indicator).Distinct().ToList();

                double totalTarget = indicators.Sum(i => i?.Target ?? 0);
                double totalAchieved = allMeasures.Sum(m => m.Value);
                double rate = totalTarget == 0 ? 0 : (totalAchieved / totalTarget) * 100;
                rate = Math.Min(rate, 100);

                return new ProjectProgressDto
                {
                    ProjectId = p.ProjectID,
                    ProjectName = p.ProjectName,
                    CompletionRate = Math.Round(rate, 2),
                    TotalIndicators = indicators.Count,
                    TotalTarget = Math.Round(totalTarget, 2),
                    TotalAchieved = Math.Round(totalAchieved, 2)
                };
            })
            .OrderByDescending(p => p.CompletionRate)
            .ToList();

            return Ok(projectProgress);
        }

        [HttpGet("indicator-trend/{code}")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<IndicatorTrendDto>> GetIndicatorTrend(int code)
        {
            var measures = await _context.Measures
                .Where(m => m.IndicatorCode == code)
                .OrderBy(m => m.Date)
                .ToListAsync();

            var indicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.IndicatorCode == code);

            var result = new IndicatorTrendDto
            {
                Real = measures.Select(m => new TrendPointDto
                {
                    Date = m.Date.ToString("yyyy-MM-dd"),
                    Value = m.Value
                }).ToList(),
                Target = new List<TrendPointDto>
                {
                    new() { Date = "baseline", Value = indicator?.Target ?? 0 }
                }
            };

            return Ok(result);
        }
    }
}
