using MonitoringAndEvaluationPlatform.Data;
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

    public async Task<IActionResult> FrameworkPerformance()
    {
        var frameworks = await _context.Frameworks.ToListAsync();
        ViewBag.Frameworks = frameworks;
        return View();
    }


    [HttpGet]
    public async Task<IActionResult> FrameworksPerformanceGauge(int? frameworkCode = null, int? ministryCode = null)
    {
        var frameworksQuery = _context.Frameworks
            .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                            .ThenInclude(i => i.Measures)
                                .ThenInclude(m => m.Project)
                                    .ThenInclude(p => p.Ministries); // include Ministry for filtering

        var frameworks = await frameworksQuery.ToListAsync();

        var result = frameworks
     .Where(fw => frameworkCode == null || fw.Code == frameworkCode)
     .Select(fw =>
     {
         var projects = fw.Outcomes
             .SelectMany(o => o.Outputs)
             .SelectMany(op => op.SubOutputs)
             .SelectMany(so => so.Indicators)
             .SelectMany(i => i.Measures)
             .Where(m => m.Project != null &&
                         (
                           ministryCode == null
                           || m.Project.Ministries.Any(min => min.Code == ministryCode)
                         )
             )
             .Select(m => m.Project)
             .Distinct()
             .ToList();

         return new
         {
             code = fw.Code,
             name = fw.Name,
             indicatorsPerformance = fw.IndicatorsPerformance,
             indicatorCount = fw.Outcomes
                 .SelectMany(o => o.Outputs)
                 .SelectMany(op => op.SubOutputs)
                 .SelectMany(so => so.Indicators)
                 .Count(),
             projects = projects
                 .Select(p => new
                 {
                     p.ProjectID,
                     p.ProjectName,
                     p.performance
                 })
                 .ToList()
         };
     });


        return Json(result);
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

    [HttpGet]
    public JsonResult GetDistrictsByGovernorates(string governorateCodes)
    {
        if (string.IsNullOrEmpty(governorateCodes))
        {
            return Json(new List<object>());
        }

        var codes = governorateCodes.Split(',');

        var districts = _context.Districts
            .Where(d => codes.Contains(d.GovernorateCode))
            .Select(d => new { d.Code, d.Name })
            .ToList();

        return Json(districts);
    }

    [HttpGet]
    public JsonResult GetDistrictsByGovernorate(string governorateCode)
    {
        var list = _context.Districts
                       .Where(d => d.GovernorateCode == governorateCode)
                       .Select(d => new { d.Code, d.Name })
                       .ToList();
        return Json(list);
    }

    [HttpGet]
    public JsonResult GetSubDistrictsByDistrict(string districtCode)
    {
        var list = _context.SubDistricts
                       .Where(s => s.DistrictCode == districtCode)
                       .Select(s => new { s.Code, s.Name })
                       .ToList();
        return Json(list);
    }

    [HttpGet]
    public JsonResult GetCommunitiesBySubDistrict(string subDistrictCode)
    {
        var list = _context.Communities
                       .Where(r => r.SubDistrictCode == subDistrictCode)
                       .Select(r => new { Code = r.SubDistrictCode, Name = r.Name })
                       .ToList();
        return Json(list);
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
            Sectors = await _context.Sectors
               .Select(s => new SelectListItem { Value = s.Code.ToString(), Text = s.Name })
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
            Frameworks = await _context.Frameworks.ToListAsync(),

            TotlalMinistries = await _context.Indicators.CountAsync(),
            Ministries = await _context.Ministries.ToListAsync(),

            TotalProjects = await _context.Projects.CountAsync(),
            Projects = await _context.Projects.ToListAsync(),

            TotalGovernorate = await _context.Governorates.CountAsync(),
            Governorates = await _context.Governorates.ToListAsync(),
            Districts = await _context.Districts.ToListAsync(),
            SubDistricts = await _context.SubDistricts.ToListAsync(),
            Communities = await _context.Communities.ToListAsync()
        };

        return View(model);
    }


    [HttpGet]
    public IActionResult ProjectProgress2(int? regionId, int? sectorId, int? donorId)
    {
        // Base query for projects
        var query = _context.Projects
            .Include(p => p.Measures)
            .Include(p => p.Donors)
            .AsQueryable();

        //// Apply filters
        //if (regionId.HasValue)
        //    query = query.Where(p => p.RegionCode == regionId);

        //To Check
        //if (sectorId.HasValue)
        //    query = query.Where(p => p.SectorCode == sectorId);

        //To Check
        //if (donorId.HasValue)
        //    query = query.Where(p => p.DonorCode == donorId);

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
            Sectors = _context.Sectors
                .Select(s => new SelectListItem { Value = s.Code.ToString(), Text = s.Name })
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

        //To check
        if (sectorId.HasValue)
            projectsQuery = projectsQuery.Where(p => p.Sectors.Any(s => s.Code == sectorId.Value));

        //To check
        if (donorId.HasValue)
            projectsQuery = projectsQuery.Where(p => p.Donors.Any(s => s.Code == donorId.Value));


        var projects = await projectsQuery.ToListAsync();

        var projectProgress = projects.Select(p =>
        {
            var measures = p.Measures;
            var indicators = measures.Select(m => m.Indicator).Distinct().ToList();

            double totalTarget = indicators.Sum(i => i?.Target ?? 0);
            double totalAchieved = measures.Where(m => m.ValueType == MeasureValueType.Real).Sum(m => m.Value);
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
            Sectors = await _context.Sectors.Select(s => new SelectListItem
            {
                Value = s.Code.ToString(),
                Text = s.Name
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
    public async Task<IActionResult> FrameworksGauge(
        int? frameworkCode,
        int? ministryCode = null,
        int? projectCode = null,
        string? governorateCode = null,
        string? districtCode = null,
        string? subDistrictCode = null,
        string? communityCode = null)
    {
        var frameworksQuery = _context.Frameworks
        .Include(f => f.Outcomes)
            .ThenInclude(o => o.Outputs)
                .ThenInclude(op => op.SubOutputs)
                    .ThenInclude(so => so.Indicators)
                        .ThenInclude(i => i.Measures)
                            .ThenInclude(m => m.Project)
                                .ThenInclude(p => p.Ministries)
        .Include(f => f.Outcomes)
            .ThenInclude(o => o.Outputs)
                .ThenInclude(op => op.SubOutputs)
                    .ThenInclude(so => so.Indicators)
                        .ThenInclude(i => i.Measures)
                            .ThenInclude(m => m.Project)
                                .ThenInclude(p => p.Governorates)
                                .ThenInclude(p => p.Districts)
                                .ThenInclude(p => p.SubDistricts)
                                .ThenInclude(p => p.Communities);


        var frameworks = await frameworksQuery.ToListAsync();

        var result = frameworks
            .Where(fw => frameworkCode == null || fw.Code == frameworkCode)
            .Select(fw =>
            {
                var allProjects = fw.Outcomes
                    .SelectMany(o => o.Outputs)
                    .SelectMany(op => op.SubOutputs)
                    .SelectMany(so => so.Indicators)
                    .SelectMany(i => i.Measures)
                    .Where(m => m.Project != null)
                    .Select(m => m.Project)
                    .Distinct()
                    .ToList();

                // Apply all filters
                var filteredProjects = allProjects
                    .Where(p =>
                        (projectCode == null || p.ProjectID == projectCode) &&
                        (ministryCode == null || p.Ministries.Any(m => m.Code == ministryCode)) &&
                        (governorateCode == null || p.Governorates.Any(m => m.Code == governorateCode)) &&
                        (districtCode == null || p.Districts.Any(m => m.Code == districtCode)) &&
                        (subDistrictCode == null || p.SubDistricts.Any(m => m.Code == districtCode)) &&
                        (communityCode == null || p.Communities.Any(m => m.Code == communityCode))
                    )
                    .ToList();


                // Determine indicatorsPerformance
                double indicatorsPerformance;
                if (projectCode != null)
                {
                    indicatorsPerformance = filteredProjects.Any()
                        ? Math.Round(filteredProjects.Average(p => p.performance), 2)
                        : 0;
                }
                else if (ministryCode != null || governorateCode != null || districtCode != null || subDistrictCode != null || communityCode != null)
                {
                    indicatorsPerformance = filteredProjects.Any()
                        ? Math.Round(filteredProjects.Average(p => p.performance), 2)
                        : 0;
                }
                else
                {
                    indicatorsPerformance = Math.Round(fw.IndicatorsPerformance, 2);
                }

                return new
                {
                    code = fw.Code,
                    name = fw.Name,
                    indicatorsPerformance,
                    indicatorCount = fw.Outcomes
                        .SelectMany(o => o.Outputs)
                        .SelectMany(op => op.SubOutputs)
                        .SelectMany(so => so.Indicators)
                        .Count(),
                    projects = filteredProjects.Select(p => new
                    {
                        p.ProjectID,
                        p.ProjectName,
                        p.performance
                    }).ToList()
                };
            });

        return Json(result);
    }


    [HttpGet]
    public async Task<IActionResult> GetMinistriesByFramework(int frameworkCode)
    {
        // Load the whole tree down to Project → Ministry
        var framework = await _context.Frameworks
            .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                            .ThenInclude(i => i.Measures)
                                .ThenInclude(m => m.Project)
                                    .ThenInclude(p => p.Ministries)        // <— include the Ministry nav prop
            .FirstOrDefaultAsync(f => f.Code == frameworkCode);

        if (framework == null)
            return Json(new List<object>());

        var ministries = framework.Outcomes
             .SelectMany(o => o.Outputs)
             .SelectMany(op => op.SubOutputs)
             .SelectMany(so => so.Indicators)
             .SelectMany(i => i.Measures)
             .Where(m => m.Project != null)
             // ↓ Flatten the collection of Ministries for each project:
             .SelectMany(m => m.Project.Ministries)
             .Distinct()   // remove duplicates
             .Select(mn => new
             {
                 id = mn.Code,         // your Ministry primary key
                 name = mn.MinistryName
             })
             .ToList();


        return Json(ministries);
    }




    [HttpGet]
    public async Task<IActionResult> GetProjectsByFramework(int frameworkCode)
    {
        var frameworksQuery = _context.Frameworks
            .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                            .ThenInclude(i => i.Measures)
                                .ThenInclude(m => m.Project);

        var framework = await frameworksQuery
            .FirstOrDefaultAsync(f => f.Code == frameworkCode);

        if (framework == null)
        {
            return Json(new List<object>());
        }

        var projects = framework.Outcomes
            .SelectMany(o => o.Outputs)
            .SelectMany(op => op.SubOutputs)
            .SelectMany(so => so.Indicators)
            .SelectMany(i => i.Measures)
            .Where(m => m.Project != null)
            .Select(m => m.Project)
            .Distinct()
            .Select(p => new
            {
                id = p.ProjectID,
                name = p.ProjectName
            })
            .ToList();

        return Json(projects);
    }



}
//totalTarget = Math.Round(totalTarget, 2),