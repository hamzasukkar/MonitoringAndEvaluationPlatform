using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize] // Only Admins can access this controller
    public class FrameworksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<HomeController> _localizer;

        public FrameworksController(ApplicationDbContext context, IStringLocalizer<HomeController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

            return LocalRedirect(returnUrl ?? "/");
        }

        // GET: Frameworks
        public async Task<IActionResult> Index(string sortOrder, string searchString, FrameworkFilterViewModel filter)
        {
            ViewData["CurrentSort"] = sortOrder;
            // هنا قمنا بتغيير الفرز الافتراضي ليكون تنازليًا حسب أداء المؤشرات
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["IndicatorsSortParm"] = sortOrder == "indicators" ? "indicators_desc" : "indicators";
            ViewData["DisbursementSortParm"] = sortOrder == "disbursement" ? "disbursement_desc" : "disbursement";
            ViewData["CurrentFilter"] = searchString;

            // Load dropdown/filter data for the ViewModel
            filter.Ministries = await _context.Ministries.ToListAsync();
            filter.Donors = await _context.Donors.ToListAsync();
            filter.Sectors = await _context.Sectors.ToListAsync();
            filter.IsMinistryUser = false; // Assuming this logic is handled elsewhere

            IQueryable<Framework> frameworksQuery = _context.Frameworks.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                frameworksQuery = frameworksQuery.Where(f => f.Name.Contains(searchString));
            }

            if (filter.SelectedMinistries != null && filter.SelectedMinistries.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.ProjectIndicators.Any(pi =>
                                        pi.Project.Ministries.Any(min => filter.SelectedMinistries.Contains(min.Code))))))));
            }

            if (filter.SelectedDonors != null && filter.SelectedDonors.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.ProjectIndicators.Any(pi =>
                                        pi.Project.Donors.Any(don => filter.SelectedDonors.Contains(don.Code))))))));
            }

            if (filter.SelectedSector != null && filter.SelectedSector.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.ProjectIndicators.Any(pi =>
                                        pi.Project.Sectors.Any(sec => filter.SelectedSector.Contains(sec.Code))))))));
            }

            // Apply sorting logic
            switch (sortOrder)
            {
                case "name_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.Name);
                    break;
                case "indicators":
                    frameworksQuery = frameworksQuery.OrderBy(f => f.IndicatorsPerformance);
                    break;
                case "indicators_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.IndicatorsPerformance);
                    break;
                case "disbursement":
                    frameworksQuery = frameworksQuery.OrderBy(f => f.DisbursementPerformance);
                    break;
                case "disbursement_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.DisbursementPerformance);
                    break;
                default:
                    // تم تغيير القيمة الافتراضية هنا
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.IndicatorsPerformance);
                    break;
            }

            filter.Frameworks = await frameworksQuery.ToListAsync();
            return View(filter);
        }

        // GET: Frameworks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var framework = await _context.Frameworks
                .FirstOrDefaultAsync(m => m.Code == id);
            if (framework == null)
            {
                return NotFound();
            }

            return View(framework);
        }

        // GET: Frameworks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Frameworks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name")] Framework framework)
        {
            ModelState.Remove(nameof(framework.Outcomes));

            if (ModelState.IsValid)
            {
                _context.Add(framework);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(framework);
        }

        // POST: Frameworks/CreateInline - AJAX endpoint for inline creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = _localizer["Framework name is required."] });
                }

                // Check if framework name already exists
                var existingFramework = await _context.Frameworks.FirstOrDefaultAsync(f => f.Name.ToLower() == name.ToLower());
                if (existingFramework != null)
                {
                    return Json(new { success = false, message = _localizer["A framework with this name already exists."] });
                }

                // Create new framework
                var framework = new Framework
                {
                    Name = name.Trim(),
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0,
                    FieldMonitoring = 0,
                    ImpactAssessment = 0
                };

                _context.Add(framework);
                await _context.SaveChangesAsync();

                // Return the created framework data for frontend update
                return Json(new
                {
                    success = true,
                    framework = new
                    {
                        code = framework.Code,
                        name = framework.Name,
                        indicatorsPerformance = Math.Round(framework.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(framework.DisbursementPerformance, 2)
                    },
                    message = _localizer["Framework created successfully!"]
                });
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use ILogger here)
                return Json(new { success = false, message = _localizer["An error occurred while creating the framework. Please try again."] });
            }
        }

        // GET: Frameworks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null)
            {
                return NotFound();
            }
            return View(framework);
        }

        // POST: Frameworks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Framework framework)
        {
            ModelState.Remove(nameof(framework.Outcomes));

            if (id != framework.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(framework);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FrameworkExists(framework.Code))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(framework);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            framework.Name = name;
            _context.Update(framework);
            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            _context.Frameworks.Remove(framework);
            await _context.SaveChangesAsync();

            return Ok();
        }



        // GET: Frameworks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var framework = await _context.Frameworks
                .FirstOrDefaultAsync(m => m.Code == id);
            if (framework == null)
            {
                return NotFound();
            }

            return View(framework);
        }

      
        private bool FrameworkExists(int id)
        {
            return _context.Frameworks.Any(e => e.Code == id);
        }

        public async Task<IActionResult>Monitoring()
        {
            return View(await _context.Frameworks.ToListAsync());
        }

        // GET: Frameworks/CreateComprehensive
        [Authorize(Roles = "Admin")]
        public IActionResult CreateComprehensive()
        {
            return View();
        }

        // POST: Frameworks/CreateComprehensive - Comprehensive framework creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateComprehensive(ComprehensiveFrameworkModel model)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    // Create Framework
                    var framework = new Framework
                    {
                        Name = model.FrameworkName.Trim(),
                        IndicatorsPerformance = 0,
                        DisbursementPerformance = 0,
                        FieldMonitoring = 0,
                        ImpactAssessment = 0
                    };

                    _context.Frameworks.Add(framework);
                    await _context.SaveChangesAsync();

                    // Create Outcomes
                    var outcomeMapping = new Dictionary<int, int>();
                    for (int i = 0; i < model.Outcomes.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Outcomes[i].Name))
                        {
                            var outcome = new Outcome
                            {
                                Name = model.Outcomes[i].Name.Trim(),
                                FrameworkCode = framework.Code,
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0,
                                FieldMonitoring = 0,
                                ImpactAssessment = 0
                            };

                            _context.Outcomes.Add(outcome);
                            await _context.SaveChangesAsync();
                            outcomeMapping[i] = outcome.Code;
                        }
                    }

                    // Create Outputs
                    var outputMapping = new Dictionary<int, int>();
                    for (int i = 0; i < model.Outputs.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Outputs[i].Name) &&
                            outcomeMapping.ContainsKey(model.Outputs[i].OutcomeIndex))
                        {
                            var output = new Output
                            {
                                Name = model.Outputs[i].Name.Trim(),
                                OutcomeCode = outcomeMapping[model.Outputs[i].OutcomeIndex],
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0,
                                FieldMonitoring = 0,
                                ImpactAssessment = 0
                            };

                            _context.Outputs.Add(output);
                            await _context.SaveChangesAsync();
                            outputMapping[i] = output.Code;
                        }
                    }

                    // Create SubOutputs
                    var subOutputMapping = new Dictionary<int, int>();
                    for (int i = 0; i < model.SubOutputs.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.SubOutputs[i].Name) &&
                            outputMapping.ContainsKey(model.SubOutputs[i].OutputIndex))
                        {
                            var subOutput = new SubOutput
                            {
                                Name = model.SubOutputs[i].Name.Trim(),
                                OutputCode = outputMapping[model.SubOutputs[i].OutputIndex],
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0,
                                FieldMonitoring = 0,
                                ImpactAssessment = 0
                            };

                            _context.SubOutputs.Add(subOutput);
                            await _context.SaveChangesAsync();
                            subOutputMapping[i] = subOutput.Code;
                        }
                    }

                    // Create Indicators
                    for (int i = 0; i < model.Indicators.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Indicators[i].Name) &&
                            subOutputMapping.ContainsKey(model.Indicators[i].SubOutputIndex))
                        {
                            var indicator = new Indicator
                            {
                                Name = model.Indicators[i].Name.Trim(),
                                SubOutputCode = subOutputMapping[model.Indicators[i].SubOutputIndex],
                                Weight = model.Indicators[i].Weight > 0 ? model.Indicators[i].Weight : 1.0,
                                Target = model.Indicators[i].Target,
                                Source = model.Indicators[i].Source?.Trim() ?? string.Empty,
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0,
                                FieldMonitoring = 0,
                                ImpactAssessment = 0,
                                IsCommon = false,
                                Active = true,
                                TargetYear = DateTime.Now.AddYears(1),
                                Concept = string.Empty,
                                Description = string.Empty
                            };

                            _context.Indicators.Add(indicator);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = _localizer["Comprehensive framework created successfully!"],
                        frameworkId = framework.Code
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use ILogger here)
                return Json(new
                {
                    success = false,
                    message = _localizer["An error occurred while creating the framework. Please try again."]
                });
            }
        }
    }

    // Models for comprehensive framework creation
    public class ComprehensiveFrameworkModel
    {
        public string FrameworkName { get; set; } = string.Empty;
        public List<OutcomeModel> Outcomes { get; set; } = new List<OutcomeModel>();
        public List<OutputModel> Outputs { get; set; } = new List<OutputModel>();
        public List<SubOutputModel> SubOutputs { get; set; } = new List<SubOutputModel>();
        public List<IndicatorModel> Indicators { get; set; } = new List<IndicatorModel>();
    }

    public class OutcomeModel
    {
        public string Name { get; set; } = string.Empty;
    }

    public class OutputModel
    {
        public string Name { get; set; } = string.Empty;
        public int OutcomeIndex { get; set; }
    }

    public class SubOutputModel
    {
        public string Name { get; set; } = string.Empty;
        public int OutputIndex { get; set; }
    }

    public class IndicatorModel
    {
        public string Name { get; set; } = string.Empty;
        public int SubOutputIndex { get; set; }
        public double Weight { get; set; } = 1.0;
        public int Target { get; set; }
        public string? Source { get; set; }
    }
}
