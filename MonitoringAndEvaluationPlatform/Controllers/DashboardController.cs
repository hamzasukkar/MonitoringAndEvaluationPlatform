using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using MonitoringAndEvaluationPlatform.ViewModel;
using MonitoringAndEvaluationPlatform.Enums;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Models;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

[Authorize]
[Permission(Permissions.ViewControlPanel)]
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
            .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                            .ThenInclude(i => i.ProjectIndicators)
                                .ThenInclude(pi => pi.Project)
                                    .ThenInclude(p => p.Ministries); // include Ministry for filtering through project indicators

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
                         (
                           ministryCode == null
                           || pi.Project.Ministries.Any(min => min.Code == ministryCode)
                         )
             )
             .Select(pi => pi.Project)
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
    public async Task<IActionResult> IndicatorTrend(int indicatorCode)
    {
        var measures = _context.Measures
            .Where(m => m.IndicatorCode == indicatorCode)
            .OrderBy(m => m.Date)
            .ToList();

        var real = measures
            .Select(m => new { date = m.Date.ToString("yyyy-MM-dd"), value = m.Value })
            .ToList();

        // Get indicator target as a single value for the chart baseline
        var indicator = await _context.Indicators
            .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorCode);
        
        var targetValue = indicator?.Target ?? 0;
        var target = new[] { new { date = "baseline", value = targetValue } };

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
            var totalAchieved = indicators.SelectMany(i => i.Measures)
                .Sum(m => m.Value);
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
            var totalAchieved = indicators.SelectMany(i => i.Measures)
                .Sum(m => m.Value);
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
            var totalAchieved = indicators.SelectMany(i => i.Measures)
                .Sum(m => m.Value);
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
            var totalAchieved = indicators.SelectMany(i => i.Measures)
                .Sum(m => m.Value);
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
            .Select(d => new { d.Code, d.EN_Name, d.AR_Name })
            .ToList();

        return Json(districts);
    }

    [HttpGet]
    public JsonResult GetSubDistrictsByDistrict(string districtCodes)
    {
        // 1. Handle null or empty input string
        if (string.IsNullOrEmpty(districtCodes))
        {
            return Json(new List<object>());
        }

        // 2. Split the comma-separated string into an array of codes
        var codes = districtCodes.Split(',');

        // 3. Use .Contains() in the Where clause to find all matching sub-districts
        var subDistricts = _context.SubDistricts
                               .Where(s => codes.Contains(s.DistrictCode))
                               .Select(s => new { s.Code, s.AR_Name })
                               .ToList();

        return Json(subDistricts);
    }

    [HttpGet]
    public JsonResult GetCommunitiesBySubDistrict(string subDistrictCodes)
    {
        // Handle null or empty input string
        if (string.IsNullOrEmpty(subDistrictCodes))
        {
            return Json(new List<object>());
        }

        // Split the comma-separated string into an array of codes
        var codes = subDistrictCodes.Split(',');

        // Use .Contains() in the Where clause and select the community's own code
        var communities = _context.Communities
                                  .Where(c => codes.Contains(c.SubDistrictCode))
                                  // Corrected from 'Code = c.SubDistrictCode' to 'Code = c.Code'
                                  // to return the unique code of the community itself.
                                  .Select(c => new { c.Code, c.AR_Name })
                                  .ToList();

        return Json(communities);
    }

    [HttpGet]
    public JsonResult GetDistrictsByGovernorate(string governorateCode)
    {
        var list = _context.Districts
                       .Where(d => d.GovernorateCode == governorateCode)
                       .Select(d => new { d.Code, d.AR_Name })
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
               .Select(m => new SelectListItem { Value = m.Code.ToString(), Text = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN })
               .ToListAsync(),
            Sectors = await _context.Sectors
               .Select(s => new SelectListItem { Value = s.Code.ToString(), Text = s.AR_Name })
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
            .Include(p => p.ProjectIndicators)
                .ThenInclude(pi => pi.Indicator)
                    .ThenInclude(i => i.Measures)
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
            TotalIndicators = p.ProjectIndicators.Select(pi => pi.IndicatorCode).Distinct().Count(),
            TotalTarget = p.ProjectIndicators.Sum(pi => pi.Indicator.Target),
            TotalAchieved = p.ProjectIndicators.SelectMany(pi => pi.Indicator.Measures)
                .Sum(m => m.Value),
            CompletionRate = p.ProjectIndicators.Sum(pi => pi.Indicator.Target) > 0
                ? (p.ProjectIndicators.SelectMany(pi => pi.Indicator.Measures)
                    .Sum(m => m.Value) / p.ProjectIndicators.Sum(pi => pi.Indicator.Target)) * 100
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
                .Select(s => new SelectListItem { Value = s.Code.ToString(), Text = s.EN_Name })
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
            .Include(p => p.ProjectIndicators)
                .ThenInclude(pi => pi.Indicator)
                    .ThenInclude(i => i.Measures)
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
            var allMeasures = p.ProjectIndicators.SelectMany(pi => pi.Indicator.Measures).ToList();
            var indicators = p.ProjectIndicators.Select(pi => pi.Indicator).Distinct().ToList();

            double totalTarget = indicators.Sum(i => i?.Target ?? 0);
            double totalAchieved = allMeasures.Sum(m => m.Value);
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
                Text = s.EN_Name
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
        //double totalAchieved = indicators.Sum(i => i.Measures.OrderByDescending(m => m.Date).FirstOrDefault()?.Value ?? 0);
        double totalAchieved = indicators.Sum(i => i.IndicatorsPerformance);

        double achievementRate = totalTarget == 0 ? 0 : (totalAchieved / totalTarget) * 100;
        achievementRate = Math.Round(Math.Min(achievementRate, 100), 2);

        return Json(new { rate = achievementRate });
    }
    [HttpGet]
    public async Task<IActionResult> FrameworksGauge(
      int? frameworkCode,
      int? ministryCode = null,
      int? projectCode = null,
      string? governorateCode = null,
      string? districtCode = null,
      string? subDistrictCode = null,
      string? communityCode = null)
    {
        var governorateCodes = governorateCode?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var districtCodes = districtCode?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var subDistrictCodes = subDistrictCode?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var communityCodes = communityCode?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        var frameworkQuery = _context.Frameworks.AsQueryable();

        // Prioritize the most specific geographic filter
        if (frameworkCode.HasValue)
        {
            frameworkQuery = frameworkQuery.Where(fw => fw.Code == frameworkCode);
        }
        else if (communityCodes?.Any() == true)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.IsEntireCountry || pi.Project.Communities.Any(c => communityCodes.Contains(c.Code))))))));
        }
        else if (subDistrictCodes?.Any() == true)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.IsEntireCountry || pi.Project.SubDistricts.Any(s => subDistrictCodes.Contains(s.Code))))))));
        }
        else if (districtCodes?.Any() == true)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.IsEntireCountry || pi.Project.Districts.Any(d => districtCodes.Contains(d.Code))))))));
        }
        else if (governorateCodes?.Any() == true)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.IsEntireCountry || pi.Project.Governorates.Any(g => governorateCodes.Contains(g.Code))))))));
        }

        // APPLY MINISTRY AND PROJECT FILTER
        if (ministryCode.HasValue)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.Ministries.Any(min => min.Code == ministryCode.Value)))))));
        }

        if (projectCode.HasValue)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.ProjectID == projectCode.Value))))));
        }

        var frameworks = await frameworkQuery
            .Select(fw => new
            {
                fw.Code,
                fw.Name,
                fw.IndicatorsPerformance,
                Indicators = fw.Outcomes
                    .SelectMany(o => o.Outputs)
                    .SelectMany(op => op.SubOutputs)
                    .SelectMany(so => so.Indicators),
                Projects = fw.Outcomes
                    .SelectMany(o => o.Outputs)
                    .SelectMany(op => op.SubOutputs)
                    .SelectMany(so => so.Indicators)
                    .SelectMany(i => i.ProjectIndicators)
                    .Select(pi => pi.Project)
                    .Where(p =>
                        p != null &&
                        (!projectCode.HasValue || p.ProjectID == projectCode.Value) &&
                        (!ministryCode.HasValue || p.Ministries.Any(m => m.Code == ministryCode.Value)) &&
                        (communityCodes == null || !communityCodes.Any() || p.IsEntireCountry || p.Communities.Any(c => communityCodes.Contains(c.Code))) &&
                        (subDistrictCodes == null || !subDistrictCodes.Any() || p.IsEntireCountry || p.SubDistricts.Any(s => subDistrictCodes.Contains(s.Code))) &&
                        (districtCodes == null || !districtCodes.Any() || p.IsEntireCountry || p.Districts.Any(d => districtCodes.Contains(d.Code))) &&
                        (governorateCodes == null || !governorateCodes.Any() || p.IsEntireCountry || p.Governorates.Any(g => governorateCodes.Contains(g.Code)))
                    )
                    .Distinct()
            })
           .OrderByDescending(f => f.IndicatorsPerformance).ToListAsync();

        var result = frameworks.Select(fw =>
        {
            double indicatorsPerformance = fw.Projects.Any()
                ? Math.Round(fw.Projects.Average(p => p.performance), 2)
                : Math.Round(fw.IndicatorsPerformance, 2);

            return new
            {
                code = fw.Code,
                name = fw.Name,
                indicatorsPerformance,
                indicatorCount = fw.Indicators.Count(),
                projects = fw.Projects.Select(p => new
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
                            .ThenInclude(i => i.ProjectIndicators)
                                .ThenInclude(pi => pi.Project)
                                    .ThenInclude(p => p.Ministries)        // <— include the Ministry nav prop
            .FirstOrDefaultAsync(f => f.Code == frameworkCode);

        if (framework == null)
            return Json(new List<object>());

        var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var ministries = framework.Outcomes
             .SelectMany(o => o.Outputs)
             .SelectMany(op => op.SubOutputs)
             .SelectMany(so => so.Indicators)
             .SelectMany(i => i.ProjectIndicators)
             .Where(pi => pi.Project != null)
             // ↓ Flatten the collection of Ministries for each project:
             .SelectMany(pi => pi.Project.Ministries)
             .Distinct()   // remove duplicates
             .Select(mn => new
             {
                 id = mn.Code,         // your Ministry primary key
                 name = currentCulture == "ar" ? mn.MinistryDisplayName_AR : mn.MinistryDisplayName_EN
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
                            .ThenInclude(i => i.ProjectIndicators)
                                .ThenInclude(pi => pi.Project);

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
            .SelectMany(i => i.ProjectIndicators)
            .Where(pi => pi.Project != null)
            .Select(pi => pi.Project)
            .Distinct()
            .Select(p => new
            {
                id = p.ProjectID,
                name = p.ProjectName
            })
            .ToList();

        return Json(projects);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectsByGovernorate(string governorateCode)
    {
        var projectsByGovernorate = await _context.Projects
            .Where(p => p.IsEntireCountry || p.Governorates.Any(g => g.Code == governorateCode))
            .Select(p => new
            {
                id = p.ProjectID,
                name = p.ProjectName
            })
            .Distinct()
            .ToListAsync();

        return Json(projectsByGovernorate);
    }

    [HttpGet]
    public async Task<IActionResult> GetMinistriesByGovernorates(string governorateCodes)
    {
        // The governorateCodes parameter will be a comma-separated string,
        // so we need to split it and parse it into a list of integers.
        var codes = governorateCodes.Split(',')
                                    .ToList();

        var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var ministries = await _context.Ministries
            .Where(m => m.Projects.Any(p => p.IsEntireCountry || p.Governorates.Any(g => codes.Contains(g.Code))))
            .Select(m => new
            {
                id = m.Code,
                name = currentCulture == "ar" ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN
            })
            .Distinct()
            .ToListAsync();

        return Json(ministries);
    }

    [HttpGet]
    public async Task<IActionResult> GetFrameworksByGovernorates(string governorateCodes)
    {
        // The governorateCodes parameter will be a comma-separated string,
        // so we need to split it and parse it into a list of integers.
        var codes = governorateCodes.Split(',')
                                    .ToList();

        var frameworks = await _context.Frameworks
            .Where(f => f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => i.ProjectIndicators.Any(pi => pi.Project.IsEntireCountry || pi.Project.Governorates.Any(g => codes.Contains(g.Code))))))))
            .Select(f => new
            {
                code = f.Code,
                name = f.Name
            })
            .Distinct()
            .ToListAsync();

        return Json(frameworks);
    }

    [HttpGet]
    public async Task<IActionResult> GetFrameworksByMinistry(int ministryCode)
    {
        var frameworks = await _context.Frameworks
            .Where(f => f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => i.ProjectIndicators.Any(pi => pi.Project.Ministries.Any(min => min.Code == ministryCode)))))))
            .Select(f => new
            {
                code = f.Code,
                name = f.Name
            })
            .Distinct()
            .ToListAsync();

        return Json(frameworks);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectsByMinistry(int ministryCode)
    {
        var projects = await _context.Projects
            .Where(p => p.Ministries.Any(m => m.Code == ministryCode))
            .Select(p => new
            {
                id = p.ProjectID,
                name = p.ProjectName
            })
            .Distinct()
            .ToListAsync();

        return Json(projects);
    }

    [HttpGet]
    public async Task<IActionResult> GetFrameworksByProject(int projectCode)
    {
        var frameworks = await _context.Frameworks
            .Where(f => f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => i.ProjectIndicators.Any(pi => pi.ProjectId == projectCode))))))
            .Select(f => new
            {
                code = f.Code,
                name = f.Name
            })
            .Distinct()
            .ToListAsync();

        return Json(frameworks);
    }

    [HttpGet]
    public async Task<IActionResult> GetMinistriesByProject(int projectCode)
    {
        var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var ministries = await _context.Ministries
            .Where(m => m.Projects.Any(p => p.ProjectID == projectCode))
            .Select(m => new
            {
                id = m.Code,
                name = currentCulture == "ar" ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN
            })
            .Distinct()
            .ToListAsync();

        return Json(ministries);
    }

    [HttpGet]
    public IActionResult GetGovernoratesByFramework(int frameworkCode)
    {
        // Collect governorates from projects that are linked via ProjectIndicators to indicators in the framework
        var governorates = _context.ProjectIndicators
            .Where(pi => pi.Indicator.SubOutput.Output.Outcome.Framework.Code == frameworkCode)
            .Where(pi => pi.Project != null)
            .SelectMany(pi => pi.Project.Governorates) // assumes Project has navigation property Governorates
            .Distinct()
            .Select(g => new
            {
                code = g.Code,
                name = g.AR_Name
            })
            .ToList();

        return Json(governorates);
    }

    [HttpGet]
    public async Task<IActionResult> GetGovernoratesByProject(int projectCode)
    {
        if (projectCode == 0)
            return Json(new List<object>());

        var governorates = await _context.Projects
            .Where(p => p.ProjectID == projectCode)
            .SelectMany(p => p.Governorates) // many-to-many navigation property
            .Select(g => new
            {
                Code = g.Code,   // adjust to your Governorate PK
                Name = g.AR_Name
            })
            .Distinct()
            .ToListAsync();

        return Json(governorates);
    }

    [HttpGet]
    public async Task<IActionResult> GetGovernoratesByMinistry(int ministryCode)
    {
        var governorates = await _context.Projects
            .Where(p => p.Ministries.Any(m => m.Code == ministryCode))
            .SelectMany(p => p.Governorates) // many-to-many Project ↔ Governorate
            .Select(g => new
            {
                code = g.Code,   // adjust if PK is GovernorateCode
                name = g.AR_Name
            })
            .Distinct()
            .ToListAsync();

        return Json(governorates);
    }

    [HttpGet]
    public async Task<IActionResult> GetFrameworksByCommunities(string communityCodes)
    {
        var codes = communityCodes?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (codes == null || !codes.Any())
            return Json(new List<object>());

        var frameworks = await _context.Frameworks
            .Where(f => f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => i.ProjectIndicators.Any(pi => pi.Project.IsEntireCountry || pi.Project.Communities.Any(c => codes.Contains(c.Code))))))))
            .Select(f => new { code = f.Code, name = f.Name })
            .Distinct()
            .ToListAsync();

        return Json(frameworks);
    }
    [HttpGet]
    public async Task<IActionResult> GetMinistriesByCommunities(string communityCodes)
    {
        var codes = communityCodes?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (codes == null || !codes.Any())
            return Json(new List<object>());

        var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var ministries = await _context.Ministries
            .Where(m => m.Projects.Any(p => p.IsEntireCountry || p.Communities.Any(c => codes.Contains(c.Code))))
            .Select(m => new { id = m.Code, name = currentCulture == "ar" ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN })
            .Distinct()
            .ToListAsync();

        return Json(ministries);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectsByCommunities(string communityCodes)
    {
        var codes = communityCodes?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (codes == null || !codes.Any())
            return Json(new List<object>());

        var projects = await _context.Projects
            .Where(p => p.IsEntireCountry || p.Communities.Any(c => codes.Contains(c.Code)))
            .Select(p => new { id = p.ProjectID, name = p.ProjectName })
            .Distinct()
            .ToListAsync();

        return Json(projects);
    }

    // GET: Dashboard/FrameworkGoalsTimeline
    [HttpGet]
    public async Task<IActionResult> FrameworkGoalsTimeline(int? goalId, int? frameworkCode)
    {
        var frameworks = await _context.Frameworks.ToListAsync();
        ViewBag.Frameworks = frameworks;
        ViewBag.SelectedGoalId = goalId;
        ViewBag.SelectedFrameworkCode = frameworkCode;
        return View();
    }

    // GET: Dashboard/GetGoalsByFramework
    [HttpGet]
    public async Task<IActionResult> GetGoalsByFramework(int frameworkCode)
    {
        var goals = await _context.FrameworkGoals
            .Where(fg => fg.FrameworkCode == frameworkCode)
            .Select(fg => new
            {
                id = fg.ID,
                name = fg.Name
            })
            .ToListAsync();

        return Json(goals);
    }

    // GET: Dashboard/GetGoalTimelineData
    [HttpGet]
    public async Task<IActionResult> GetGoalTimelineData(int goalId)
    {
        var goal = await _context.FrameworkGoals
            .Include(fg => fg.Framework)
            .Include(fg => fg.YearlyValues)
            .FirstOrDefaultAsync(fg => fg.ID == goalId);

        if (goal == null)
        {
            return NotFound();
        }

        // Generate time series data from StartingYear to TargetYear
        // Use AnnualChangeRate (signed) for correct calculation of both increase and decrease goals
        var timeSeriesData = new List<object>();

        for (int year = goal.StartingYear; year <= goal.TargetYear; year++)
        {
            var yearsPassed = year - goal.StartingYear;
            var annualChangeRate = goal.AnnualChangeRate;
            var change = annualChangeRate * yearsPassed;
            var expectedValue = goal.BaseValueForStartingYear + change;

            timeSeriesData.Add(new
            {
                year = year,
                annualChangeRate = Math.Round(annualChangeRate, 2),
                amountOfChange = Math.Round(Math.Abs(change), 2),
                expectedValue = Math.Round(expectedValue, 2)
            });
        }

        // Get historical yearly values
        var yearlyValues = goal.YearlyValues
            .OrderBy(yv => yv.Year)
            .Select(yv => new
            {
                year = yv.Year,
                actualValue = Math.Round(yv.ActualValue, 2),
                dateRecorded = yv.DateRecorded.ToString("yyyy-MM-dd")
            })
            .ToList();

        var result = new
        {
            goalId = goal.ID,
            goalName = goal.Name,
            frameworkCode = goal.FrameworkCode,
            frameworkName = goal.Framework?.Name ?? "Unknown",
            startingYear = goal.StartingYear,
            currentYear = goal.CurrentYear,
            targetYear = goal.TargetYear,
            baseValue = goal.BaseValueForStartingYear,
            currentValue = goal.BaseValueForCurrentYear,
            targetValue = goal.TargetValue,
            isIncreaseGoal = goal.IsIncreaseGoal,
            annualChangeRate = Math.Round(goal.AnnualChangeRate, 2),
            annualDiscountRate = Math.Round(goal.AnnualDiscountRate, 2),
            currentReduction = Math.Round(goal.AmountOfReduction, 2),
            expectedCurrentValue = Math.Round(goal.ExpectedValueForCurrentYear, 2),
            progressRate = Math.Round(goal.ProgressRate, 2),
            timeSeries = timeSeriesData,
            yearlyValues = yearlyValues
        };

        return Json(result);
    }


    [HttpPost]
    public async Task<IActionResult> ExportExcel([FromBody] DashboardExportRequest? request)
    {
        request ??= new DashboardExportRequest();

        var exportData = await GetExportData(request.FrameworkCode, request.MinistryCode, request.ProjectCode,
            request.GovernorateCode, request.DistrictCode, request.SubDistrictCode, request.CommunityCode);

        using var workbook = new XLWorkbook();

        // Summary Sheet
        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell(1, 1).Value = "Dashboard Export Summary";
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 16;
        summarySheet.Range(1, 1, 1, 4).Merge();

        summarySheet.Cell(3, 1).Value = "Export Date:";
        summarySheet.Cell(3, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        summarySheet.Cell(4, 1).Value = "Total Frameworks:";
        summarySheet.Cell(4, 2).Value = exportData.Frameworks.Count;
        summarySheet.Cell(5, 1).Value = "Total Projects:";
        summarySheet.Cell(5, 2).Value = exportData.Frameworks.SelectMany(f => f.Projects).Distinct().Count();

        summarySheet.Columns().AdjustToContents();

        // Framework Performance Sheet
        var frameworkSheet = workbook.Worksheets.Add("Framework Performance");
        frameworkSheet.Cell(1, 1).Value = "Framework";
        frameworkSheet.Cell(1, 2).Value = "Performance (%)";
        frameworkSheet.Cell(1, 3).Value = "Indicator Count";
        frameworkSheet.Cell(1, 4).Value = "Project Count";

        var headerRange = frameworkSheet.Range(1, 1, 1, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#667eea");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var fw in exportData.Frameworks)
        {
            frameworkSheet.Cell(row, 1).Value = fw.Name;
            frameworkSheet.Cell(row, 2).Value = Math.Round(fw.Performance, 2);
            frameworkSheet.Cell(row, 3).Value = fw.IndicatorCount;
            frameworkSheet.Cell(row, 4).Value = fw.Projects.Count;
            row++;
        }

        frameworkSheet.Columns().AdjustToContents();

        // Projects Sheet
        var projectsSheet = workbook.Worksheets.Add("Projects");
        projectsSheet.Cell(1, 1).Value = "Framework";
        projectsSheet.Cell(1, 2).Value = "Project Name";
        projectsSheet.Cell(1, 3).Value = "Performance (%)";

        var projectHeaderRange = projectsSheet.Range(1, 1, 1, 3);
        projectHeaderRange.Style.Font.Bold = true;
        projectHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#667eea");
        projectHeaderRange.Style.Font.FontColor = XLColor.White;

        row = 2;
        foreach (var fw in exportData.Frameworks)
        {
            foreach (var proj in fw.Projects)
            {
                projectsSheet.Cell(row, 1).Value = fw.Name;
                projectsSheet.Cell(row, 2).Value = proj.ProjectName;
                projectsSheet.Cell(row, 3).Value = Math.Round(proj.Performance, 2);
                row++;
            }
        }

        projectsSheet.Columns().AdjustToContents();

        // Charts Sheet - Add chart images if available
        if (request.ChartImages?.Any() == true)
        {
            var chartsSheet = workbook.Worksheets.Add("Charts");
            chartsSheet.Cell(1, 1).Value = "Framework Performance Charts";
            chartsSheet.Cell(1, 1).Style.Font.Bold = true;
            chartsSheet.Cell(1, 1).Style.Font.FontSize = 16;

            int imageRow = 3;
            int chartIndex = 1;
            foreach (var chart in request.ChartImages)
            {
                try
                {
                    // Add chart title
                    chartsSheet.Cell(imageRow, 1).Value = $"{chartIndex}. {chart.Name}";
                    chartsSheet.Cell(imageRow, 1).Style.Font.Bold = true;
                    chartsSheet.Cell(imageRow + 1, 1).Value = chart.Performance;

                    // Convert base64 to image and add to sheet
                    if (!string.IsNullOrEmpty(chart.Image) && chart.Image.Contains(","))
                    {
                        var base64Data = chart.Image.Split(',')[1];
                        var imageBytes = Convert.FromBase64String(base64Data);
                        using var imageStream = new MemoryStream(imageBytes);

                        var picture = chartsSheet.AddPicture(imageStream)
                            .MoveTo(chartsSheet.Cell(imageRow + 2, 1))
                            .WithSize(200, 200);
                    }

                    imageRow += 18; // Space for next chart
                    chartIndex++;
                }
                catch (Exception)
                {
                    // Skip invalid images
                    continue;
                }
            }

            chartsSheet.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Dashboard_Export_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> ExportPdf([FromBody] DashboardExportRequest? request)
    {
        request ??= new DashboardExportRequest();

        QuestPDF.Settings.License = LicenseType.Community;

        var exportData = await GetExportData(request.FrameworkCode, request.MinistryCode, request.ProjectCode,
            request.GovernorateCode, request.DistrictCode, request.SubDistrictCode, request.CommunityCode);

        var isRtl = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        // Pre-process chart images
        var chartImageBytes = new List<(string Name, string Performance, byte[] ImageData)>();
        if (request.ChartImages?.Any() == true)
        {
            foreach (var chart in request.ChartImages)
            {
                try
                {
                    if (!string.IsNullOrEmpty(chart.Image) && chart.Image.Contains(","))
                    {
                        var base64Data = chart.Image.Split(',')[1];
                        var imageBytes = Convert.FromBase64String(base64Data);
                        chartImageBytes.Add((chart.Name, chart.Performance, imageBytes));
                    }
                }
                catch
                {
                    // Skip invalid images
                }
            }
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                if (isRtl)
                {
                    page.ContentFromRightToLeft();
                }

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Text(isRtl ? "ملخص لوحة المعلومات" : "Dashboard Summary Report")
                        .FontSize(20).Bold().FontColor(Colors.Indigo.Darken2);
                    col.Item().Text($"{(isRtl ? "تاريخ التصدير:" : "Export Date:")} {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // Content
                page.Content().Column(col =>
                {
                    // Summary Section
                    col.Item().PaddingVertical(10).Text(isRtl ? "ملخص" : "Summary")
                        .FontSize(14).Bold().FontColor(Colors.Indigo.Darken1);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                        });

                        table.Cell().Padding(5).Text(isRtl ? "إجمالي الأطر:" : "Total Frameworks:");
                        table.Cell().Padding(5).Text(exportData.Frameworks.Count.ToString());

                        table.Cell().Padding(5).Text(isRtl ? "إجمالي المشاريع:" : "Total Projects:");
                        table.Cell().Padding(5).Text(exportData.Frameworks.SelectMany(f => f.Projects).Distinct().Count().ToString());
                    });

                    col.Item().PaddingVertical(5);

                    // Charts Section - Add chart images if available
                    if (chartImageBytes.Any())
                    {
                        col.Item().PaddingVertical(10).Text(isRtl ? "مخططات الأداء" : "Performance Charts")
                            .FontSize(14).Bold().FontColor(Colors.Indigo.Darken1);

                        col.Item().Row(row =>
                        {
                            int chartCount = 0;
                            foreach (var chart in chartImageBytes)
                            {
                                if (chartCount > 0 && chartCount % 2 == 0)
                                {
                                    // This will be handled by creating new rows
                                }

                                row.RelativeItem().Padding(5).Column(chartCol =>
                                {
                                    chartCol.Item().Text(chart.Name).Bold().FontSize(10);
                                    chartCol.Item().Text(chart.Performance).FontSize(9).FontColor(Colors.Green.Darken1);
                                    chartCol.Item().Padding(5).Image(chart.ImageData).FitWidth();
                                });

                                chartCount++;
                                if (chartCount >= 2) break; // Only show first 2 in this row
                            }
                        });

                        // Additional charts in new rows
                        for (int i = 2; i < chartImageBytes.Count; i += 2)
                        {
                            col.Item().Row(row =>
                            {
                                for (int j = i; j < Math.Min(i + 2, chartImageBytes.Count); j++)
                                {
                                    var chart = chartImageBytes[j];
                                    row.RelativeItem().Padding(5).Column(chartCol =>
                                    {
                                        chartCol.Item().Text(chart.Name).Bold().FontSize(10);
                                        chartCol.Item().Text(chart.Performance).FontSize(9).FontColor(Colors.Green.Darken1);
                                        chartCol.Item().Padding(5).Image(chart.ImageData).FitWidth();
                                    });
                                }

                                // Fill empty space if odd number
                                if (i + 1 >= chartImageBytes.Count)
                                {
                                    row.RelativeItem();
                                }
                            });
                        }

                        col.Item().PaddingVertical(10);
                    }

                    // Framework Performance Section
                    col.Item().PaddingVertical(10).Text(isRtl ? "أداء الاستراتيجيات" : "Framework Performance")
                        .FontSize(14).Bold().FontColor(Colors.Indigo.Darken1);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                .Text(isRtl ? "الإطار" : "Framework").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                .Text(isRtl ? "الأداء (%)" : "Performance (%)").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                .Text(isRtl ? "المؤشرات" : "Indicators").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                .Text(isRtl ? "المشاريع" : "Projects").FontColor(Colors.White).Bold();
                        });

                        // Data rows
                        foreach (var fw in exportData.Frameworks)
                        {
                            var bgColor = exportData.Frameworks.ToList().IndexOf(fw) % 2 == 0
                                ? Colors.White : Colors.Grey.Lighten4;

                            table.Cell().Background(bgColor).Padding(5).Text(fw.Name);
                            table.Cell().Background(bgColor).Padding(5).Text($"{Math.Round(fw.Performance, 2)}%");
                            table.Cell().Background(bgColor).Padding(5).Text(fw.IndicatorCount.ToString());
                            table.Cell().Background(bgColor).Padding(5).Text(fw.Projects.Count.ToString());
                        }
                    });

                    col.Item().PaddingVertical(10);

                    // Projects Section
                    col.Item().PaddingVertical(10).Text(isRtl ? "تفاصيل المشاريع" : "Project Details")
                        .FontSize(14).Bold().FontColor(Colors.Indigo.Darken1);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                .Text(isRtl ? "الإطار" : "Framework").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                .Text(isRtl ? "المشروع" : "Project").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                .Text(isRtl ? "الأداء (%)" : "Performance (%)").FontColor(Colors.White).Bold();
                        });

                        int rowIndex = 0;
                        foreach (var fw in exportData.Frameworks)
                        {
                            foreach (var proj in fw.Projects)
                            {
                                var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                table.Cell().Background(bgColor).Padding(5).Text(fw.Name);
                                table.Cell().Background(bgColor).Padding(5).Text(proj.ProjectName);
                                table.Cell().Background(bgColor).Padding(5).Text($"{Math.Round(proj.Performance, 2)}%");
                                rowIndex++;
                            }
                        }
                    });
                });

                // Footer
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span(isRtl ? "صفحة " : "Page ");
                    text.CurrentPageNumber();
                    text.Span(isRtl ? " من " : " of ");
                    text.TotalPages();
                });
            });
        });

        var pdfBytes = document.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"Dashboard_Export_{DateTime.Now:yyyyMMdd}.pdf");
    }

    private async Task<DashboardExportData> GetExportData(
        int? frameworkCode,
        int? ministryCode,
        int? projectCode,
        string? governorateCode,
        string? districtCode,
        string? subDistrictCode,
        string? communityCode)
    {
        var governorateCodes = governorateCode?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var districtCodes = districtCode?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var subDistrictCodes = subDistrictCode?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var communityCodes = communityCode?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        var frameworkQuery = _context.Frameworks.AsQueryable();

        if (frameworkCode.HasValue)
        {
            frameworkQuery = frameworkQuery.Where(fw => fw.Code == frameworkCode);
        }
        else if (communityCodes?.Any() == true)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.IsEntireCountry || pi.Project.Communities.Any(c => communityCodes.Contains(c.Code))))))));
        }
        else if (subDistrictCodes?.Any() == true)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.IsEntireCountry || pi.Project.SubDistricts.Any(s => subDistrictCodes.Contains(s.Code))))))));
        }
        else if (districtCodes?.Any() == true)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.IsEntireCountry || pi.Project.Districts.Any(d => districtCodes.Contains(d.Code))))))));
        }
        else if (governorateCodes?.Any() == true)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.IsEntireCountry || pi.Project.Governorates.Any(g => governorateCodes.Contains(g.Code))))))));
        }

        if (ministryCode.HasValue)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.Ministries.Any(min => min.Code == ministryCode.Value)))))));
        }

        if (projectCode.HasValue)
        {
            frameworkQuery = frameworkQuery.Where(f =>
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.ProjectIndicators.Any(pi =>
                                    pi.Project.ProjectID == projectCode.Value))))));
        }

        var frameworks = await frameworkQuery
            .Select(fw => new
            {
                fw.Code,
                fw.Name,
                fw.IndicatorsPerformance,
                Indicators = fw.Outcomes
                    .SelectMany(o => o.Outputs)
                    .SelectMany(op => op.SubOutputs)
                    .SelectMany(so => so.Indicators),
                Projects = fw.Outcomes
                    .SelectMany(o => o.Outputs)
                    .SelectMany(op => op.SubOutputs)
                    .SelectMany(so => so.Indicators)
                    .SelectMany(i => i.ProjectIndicators)
                    .Select(pi => pi.Project)
                    .Where(p =>
                        p != null &&
                        (!projectCode.HasValue || p.ProjectID == projectCode.Value) &&
                        (!ministryCode.HasValue || p.Ministries.Any(m => m.Code == ministryCode.Value)) &&
                        (communityCodes == null || !communityCodes.Any() || p.IsEntireCountry || p.Communities.Any(c => communityCodes.Contains(c.Code))) &&
                        (subDistrictCodes == null || !subDistrictCodes.Any() || p.IsEntireCountry || p.SubDistricts.Any(s => subDistrictCodes.Contains(s.Code))) &&
                        (districtCodes == null || !districtCodes.Any() || p.IsEntireCountry || p.Districts.Any(d => districtCodes.Contains(d.Code))) &&
                        (governorateCodes == null || !governorateCodes.Any() || p.IsEntireCountry || p.Governorates.Any(g => governorateCodes.Contains(g.Code)))
                    )
                    .Distinct()
            })
            .OrderByDescending(f => f.IndicatorsPerformance)
            .ToListAsync();

        var exportData = new DashboardExportData
        {
            Frameworks = frameworks.Select(fw =>
            {
                double performance = fw.Projects.Any()
                    ? fw.Projects.Average(p => p.performance)
                    : fw.IndicatorsPerformance;

                return new FrameworkExportItem
                {
                    Code = fw.Code,
                    Name = fw.Name,
                    Performance = performance,
                    IndicatorCount = fw.Indicators.Count(),
                    Projects = fw.Projects.Select(p => new ProjectExportItem
                    {
                        ProjectID = p.ProjectID,
                        ProjectName = p.ProjectName,
                        Performance = p.performance
                    }).ToList()
                };
            }).ToList()
        };

        return exportData;
    }
}

public class DashboardExportData
{
    public List<FrameworkExportItem> Frameworks { get; set; } = new();
}

public class FrameworkExportItem
{
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Performance { get; set; }
    public int IndicatorCount { get; set; }
    public List<ProjectExportItem> Projects { get; set; } = new();
}

public class ProjectExportItem
{
    public int ProjectID { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public double Performance { get; set; }
}

public class DashboardExportRequest
{
    public int? FrameworkCode { get; set; }
    public int? MinistryCode { get; set; }
    public int? ProjectCode { get; set; }
    public string? GovernorateCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? SubDistrictCode { get; set; }
    public string? CommunityCode { get; set; }
    public List<ChartImageData>? ChartImages { get; set; }
}

public class ChartImageData
{
    public string Name { get; set; } = string.Empty;
    public string Performance { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}
//totalTarget = Math.Round(totalTarget, 2),