﻿using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using MonitoringAndEvaluationPlatform.ViewModel;
using MonitoringAndEvaluationPlatform.Enums;

public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpGet]
    public IActionResult IndicatorTrend(int indicatorCode)
    {
        var measures = _context.Measures
            .Where(m => m.IndicatorCode == indicatorCode)
            .OrderBy(m => m.Date)
            .ToList();

        var real = measures
            .Where(m => m.ValueType == MeasureValueType.Real)
            .Select(m => new { date = m.Date.ToString("yyyy-MM-dd"), value = m.Value })
            .ToList();

        var target = measures
            .Where(m => m.ValueType == MeasureValueType.Target)
            .Select(m => new { date = m.Date.ToString("yyyy-MM-dd"), value = m.Value })
            .ToList();

        return Json(new { real, target });
    }


    [HttpGet]
    public IActionResult OutcomeProgress(int? frameworkCode)
    {
        // For the dropdown
        ViewBag.Frameworks = _context.Frameworks
            .Select(f => new SelectListItem
            {
                Value = f.Code.ToString(),
                Text = f.Name
            }).ToList();

        var outcomesQuery = _context.Outcomes
            .Include(o => o.Outputs)
                .ThenInclude(op => op.SubOutputs)
                    .ThenInclude(so => so.Indicators)
                        .ThenInclude(i => i.Measures)
            .AsQueryable();

        if (frameworkCode.HasValue)
            outcomesQuery = outcomesQuery.Where(o => o.FrameworkCode == frameworkCode.Value);

        var outcomes = outcomesQuery.ToList();

        var items = outcomes.Select(o =>
        {
            var indicators = o.Outputs
                .SelectMany(op => op.SubOutputs)
                .SelectMany(so => so.Indicators)
                .ToList();

            var totalTarget = indicators.Sum(i => i.Target);
            var totalAchieved = indicators.SelectMany(i => i.Measures).Sum(m => m.Value);
            var achievementRate = totalTarget > 0 ? (totalAchieved / totalTarget) * 100 : 0;

            return new OutcomeProgressItem
            {
                OutcomeName = o.Name,
                TotalIndicators = indicators.Count,
                TotalTarget = totalTarget,
                TotalAchieved = totalAchieved,
                AchievementRate = achievementRate
            };
        })
        .OrderByDescending(x => x.AchievementRate)
        .ToList();

        return View(new OutcomeProgressViewModel
        {
            Outcomes = items
        });
    }
    public IActionResult FrameworkOutcomeDashboard(int? frameworkCode)
    {
        var allFrameworks = _context.Frameworks
            .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                            .ThenInclude(i => i.Measures)
            .ToList();

        var frameworkItems = allFrameworks.Select(f =>
        {
            var indicators = f.Outcomes
                .SelectMany(o => o.Outputs)
                .SelectMany(op => op.SubOutputs)
                .SelectMany(so => so.Indicators)
                .ToList();

            var totalTarget = indicators.Sum(i => i.Target);
            var totalAchieved = indicators.SelectMany(i => i.Measures).Sum(m => m.Value);
            var rate = totalTarget > 0 ? (totalAchieved / totalTarget) * 100 : 0;

            return new FrameworkProgressItem
            {
                FrameworkName = f.Name,
                AchievementRate = rate,
                TotalIndicators = indicators.Count,
                TotalTarget = totalTarget,
                TotalAchieved = totalAchieved
            };
        }).OrderByDescending(f => f.AchievementRate).ToList();

        var filteredOutcomes = allFrameworks
            .Where(f => !frameworkCode.HasValue || f.Code == frameworkCode.Value)
            .SelectMany(f => f.Outcomes)
            .ToList();

        var outcomeItems = filteredOutcomes.Select(o =>
        {
            var indicators = o.Outputs
                .SelectMany(op => op.SubOutputs)
                .SelectMany(so => so.Indicators)
                .ToList();

            var totalTarget = indicators.Sum(i => i.Target);
            var totalAchieved = indicators.SelectMany(i => i.Measures).Sum(m => m.Value);
            var rate = totalTarget > 0 ? (totalAchieved / totalTarget) * 100 : 0;

            return new OutcomeProgressItem
            {
                OutcomeName = o.Name,
                AchievementRate = rate,
                TotalIndicators = indicators.Count,
                TotalTarget = totalTarget,
                TotalAchieved = totalAchieved
            };
        }).OrderByDescending(o => o.AchievementRate).ToList();

        var model = new FrameworkOutcomeDashboardViewModel
        {
            Frameworks = frameworkItems,
            Outcomes = outcomeItems,
            SelectedFrameworkCode = frameworkCode,
            FrameworkOptions = _context.Frameworks
                .Select(f => new SelectListItem
                {
                    Value = f.Code.ToString(),
                    Text = f.Name
                }).ToList()
        };

        return View(model);
    }



    [HttpGet]
    public IActionResult FrameworkProgress()
    {
        var frameworks = _context.Frameworks
            .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                            .ThenInclude(i => i.Measures)
            .ToList();

        var items = frameworks.Select(f =>
        {
            var indicators = f.Outcomes
                .SelectMany(o => o.Outputs)
                .SelectMany(op => op.SubOutputs)
                .SelectMany(so => so.Indicators)
                .ToList();

            var totalTarget = indicators.Sum(i => i.Target);
            var totalAchieved = indicators.SelectMany(i => i.Measures).Sum(m => m.Value);
            var achievementRate = totalTarget > 0 ? (totalAchieved / totalTarget) * 100 : 0;

            return new FrameworkProgressItem
            {
                FrameworkName = f.Name,
                TotalIndicators = indicators.Count,
                TotalTarget = totalTarget,
                TotalAchieved = totalAchieved,
                AchievementRate = achievementRate
            };
        })
        .OrderByDescending(x => x.AchievementRate)
        .ToList();

        return View(new FrameworkProgressViewModel { Frameworks = items });
    }


    public async Task<IActionResult> Gauge()
    {
        var frameworks = await _context.Frameworks.ToListAsync();
        return View(frameworks);
    }



    [HttpGet]
    public async Task<IActionResult> GetFrameworkAchievement(int id)
    {
        var framework = await _context.Frameworks.FindAsync(id);
        if (framework == null)
            return NotFound();

        double rate = framework.IndicatorsPerformance; // Your logic

        return Json(new
        {
            name = framework.Name,
            achievement = rate
        });
    }



    public async Task<IActionResult> Test4()
    {
        var viewModel = new DataSuggestionViewModel
        {
            Frameworks = await _context.Frameworks
               .Select(f => new SelectListItem { Value = f.Code.ToString(), Text = f.Name })
               .ToListAsync(),

            Outcomes = await _context.Outcomes
               .Select(o => new SelectListItem { Value = o.Code.ToString(), Text = o.Name })
               .ToListAsync(),

            Outputs = await _context.Outputs
               .Select(o => new SelectListItem { Value = o.Code.ToString(), Text = o.Name })
               .ToListAsync(),

            SubOutputs = await _context.SubOutputs
               .Select(s => new SelectListItem { Value = s.Code.ToString(), Text = s.Name })
               .ToListAsync(),

            Indicators = await _context.Indicators
               .Select(i => new SelectListItem { Value = i.IndicatorCode.ToString(), Text = i.Name })
               .ToListAsync(),

            Projects = await _context.Projects
               .Select(p => new SelectListItem { Value = p.ProjectID.ToString(), Text = p.ProjectName })
               .ToListAsync(),

            Ministries = await _context.Ministries
               .Select(m => new SelectListItem { Value = m.Code.ToString(), Text = m.MinistryName })
               .ToListAsync(),

            Regions = await _context.Regions
               .Select(r => new SelectListItem { Value = r.Code.ToString(), Text = r.Name })
               .ToListAsync(),

            Sectors = await _context.Sectors
               .Select(s => new SelectListItem { Value = s.Code.ToString(), Text = s.Partner })
               .ToListAsync(),

            Donors = await _context.Donors
               .Select(d => new SelectListItem { Value = d.Code.ToString(), Text = d.Partner })
               .ToListAsync(),

            // Sample chart data (replace with real logic later)
            ChartItems = new List<ChartItem>
            {
                new ChartItem { ID=1,Label="Lable 1",Title = "Health", Actual = 420360, Target = 15000000, Unit = "K" },
                new ChartItem { ID=1,Label="Lable 2",Title = "Education", Actual = 3650000, Target = 15000000, Unit = "M" },
                new ChartItem { ID=1,Label="Lable 3",Title = "Agriculture", Actual = 4070000, Target = 15000000, Unit = "M" }
            }
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Test()
    {
        return View();
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardSummaryViewModel
        {
            TotalFrameworks = await _context.Frameworks.CountAsync(),
            TotalIndicators = await _context.Indicators.CountAsync(),
            TotalProjects = await _context.Projects.CountAsync(),
            TotalRegions = await _context.Regions.CountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult ProjectProgress2(int? regionId, int? sectorId, int? donorId)
    {
        // Base query for projects
        var query = _context.Projects
            .Include(p => p.Measures)
            .Include(p => p.Region)
            .Include(p => p.Donor)
            .AsQueryable();

        // Apply filters
        if (regionId.HasValue)
            query = query.Where(p => p.RegionCode == regionId);

        if (sectorId.HasValue)
            query = query.Where(p => p.RegionCode == sectorId);

        if (donorId.HasValue)
            query = query.Where(p => p.DonorCode == donorId);

        // Project progress list
        var projectList = query.Select(p => new ProjectProgressItem
        {
            ProjectName = p.ProjectName,
            TotalIndicators = p.Measures.Select(m => m.IndicatorCode).Distinct().Count(),
            TotalTarget = p.Measures.Sum(m => m.Indicator.Target),
            TotalAchieved = p.Measures.Sum(m => m.Value),
            CompletionRate = p.Measures.Sum(m => m.Indicator.Target) > 0
                ? (p.Measures.Sum(m => m.Value) / p.Measures.Sum(m => m.Indicator.Target)) * 100
                : 0
        })
        .ToList();

        // ViewModel
        var viewModel = new ProjectProgress2ViewModel
        {
            RegionId = regionId,
            SectorId = sectorId,
            DonorId = donorId,
            Projects = projectList,
            Regions = _context.Regions
                .Select(r => new SelectListItem { Value = r.Code.ToString(), Text = r.Name })
                .ToList(),
            Sectors = _context.Sectors
                .Select(s => new SelectListItem { Value = s.Code.ToString(), Text = s.Partner })
                .ToList(),
            Donors = _context.Donors
                .Select(d => new SelectListItem { Value = d.Code.ToString(), Text = d.Partner })
                .ToList()
        };

        return View(viewModel);
    }


    public async Task<IActionResult> ProjectProgress(int? regionId, int? sectorId, int? donorId)
    {
        var projectsQuery = _context.Projects
            .Include(p => p.Measures)
            .ThenInclude(m => m.Indicator)
            .AsQueryable();

        if (regionId.HasValue)
            projectsQuery = projectsQuery.Where(p => p.RegionCode == regionId);

        if (sectorId.HasValue)
            projectsQuery = projectsQuery.Where(p => p.RegionCode == sectorId);

        if (donorId.HasValue)
            projectsQuery = projectsQuery.Where(p => p.DonorCode == donorId);

        var projects = await projectsQuery.ToListAsync();

        var projectProgress = projects.Select(p =>
        {
            var measures = p.Measures;
            var indicators = measures.Select(m => m.Indicator).Distinct().ToList();

            double totalTarget = indicators.Sum(i => i?.Target ?? 0);
            double totalAchieved = measures.Where(m=>m.ValueType==MeasureValueType.Real).Sum(m => m.Value);
            double rate = totalTarget == 0 ? 0 : (totalAchieved / totalTarget) * 100;
            rate = Math.Min(rate, 100);

            return new ProjectProgressViewModel
            {
                ProjectId = p.ProjectID,
                ProjectName = p.ProjectName,
                CompletionRate = Math.Round(rate, 2),
                TotalIndicators = indicators.Count,
                TotalTarget = Math.Round(totalTarget, 2),
                TotalAchieved = Math.Round(totalAchieved, 2)
            };
        }).OrderByDescending(p => p.CompletionRate).ToList();

        var model = new ProjectProgressFilterViewModel
        {
            RegionId = regionId,
            SectorId = sectorId,
            DonorId = donorId,
            Regions = await _context.Regions.Select(r => new SelectListItem
            {
                Value = r.Code.ToString(),
                Text = r.Name
            }).ToListAsync(),
            Sectors = await _context.Sectors.Select(s => new SelectListItem
            {
                Value = s.Code.ToString(),
                Text = s.Partner
            }).ToListAsync(),
            Donors = await _context.Donors.Select(d => new SelectListItem
            {
                Value = d.Code.ToString(),
                Text = d.Partner
            }).ToListAsync(),
            Projects = projectProgress
        };

        return View(model);
    }


    public async Task<IActionResult> FrameworkGauge(int frameworkCode)
    {
        var framework = await _context.Frameworks
            .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                        .ThenInclude(so => so.Indicators)
            .FirstOrDefaultAsync(f => f.Code == frameworkCode);

        if (framework == null)
            return NotFound();

        // Aggregate all indicators under this framework
        var indicators = framework.Outcomes
            .SelectMany(o => o.Outputs)
            .SelectMany(op => op.SubOutputs)
            .SelectMany(so => so.Indicators)
            .ToList();

        double totalTarget = indicators.Sum(i => i.Target);
        //double totalAchieved = indicators.Sum(i => i.Measures.OrderByDescending(m => m.Date).FirstOrDefault(m=>m.ValueType==MeasureValueType.Real)?.Value ?? 0);
        double totalAchieved = indicators.Sum(i => i.IndicatorsPerformance);

        double achievementRate = totalTarget == 0 ? 0 : (totalAchieved / totalTarget) * 100;
        achievementRate = Math.Round(Math.Min(achievementRate, 100), 2);

        return Json(new { rate = achievementRate });
    }

    [HttpGet]
  public async Task<IActionResult> FrameworksGauge()
{
    var frameworks = await _context.Frameworks
        .Include(f => f.Outcomes)
            .ThenInclude(o => o.Outputs)
                .ThenInclude(op => op.SubOutputs)
                    .ThenInclude(so => so.Indicators)
        .ToListAsync();

    var result = frameworks.Select(fw =>
    {
        var indicators = fw.Outcomes
            .SelectMany(o => o.Outputs)
            .SelectMany(op => op.SubOutputs)
            .SelectMany(so => so.Indicators)
            .ToList();

        double totalTarget = indicators.Sum(i => i.Target);
        double totalAchieved = indicators.Sum(i => i.IndicatorsPerformance);
        double rate = totalTarget == 0 ? 0 : (totalAchieved / totalTarget) * 100;
        rate = Math.Round(Math.Min(rate, 100), 2);

        return new {
            code = fw.Code,
            name = fw.Name,
            rate,
            totalTarget = Math.Round(totalTarget, 2),
            totalAchieved = Math.Round(totalAchieved, 2),
            indicatorCount = indicators.Count
        };
    });

    return Json(result);
}



}
