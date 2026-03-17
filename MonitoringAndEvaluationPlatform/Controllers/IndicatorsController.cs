using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Localization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class IndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PlanService _planService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<IndicatorsController> _localizer;

        public IndicatorsController(ApplicationDbContext context, PlanService planService, UserManager<ApplicationUser> userManager, IStringLocalizer<IndicatorsController> localizer)
        {
            _context = context;
            _planService = planService;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: Indicators
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Index(int? frameworkCode, int? subOutputCode, string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["subOutputCode"] = subOutputCode;
            ViewData["frameworkCode"] = frameworkCode;

            // Get current user and check if they are a ministry user
            var currentUser = await _userManager.GetUserAsync(User);
            var isMinistryUser = User.IsInRole(UserRoles.MinistriesUser) || User.IsInRole(UserRoles.DataEntry);
            var userMinistryName = currentUser?.MinistryName;

            var indicators = _context.Indicators
                .Include(i => i.SubOutput)
                    .ThenInclude(so => so.Output)
                    .ThenInclude(o => o.Outcome)
                    .ThenInclude(oc => oc.Framework)
                .Include(i => i.Project)
                    .ThenInclude(p => p.Ministry)
                .Include(i => i.Project)
                    .ThenInclude(p => p.Phases)
                .AsQueryable();

            // Apply framework filter
            if (frameworkCode.HasValue)
            {
                indicators = indicators.Where(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode);
            }

            // Apply subOutput filter
            if (subOutputCode.HasValue)
            {
                indicators = indicators.Where(i => i.SubOutputCode == subOutputCode);
            }

            // Filter by ministry if user is a ministry user
            if (isMinistryUser && !string.IsNullOrEmpty(userMinistryName))
            {
                indicators = indicators.Where(i =>
                    i.Project != null && i.Project.Ministry != null &&
                    (i.Project.Ministry.MinistryDisplayName_AR == userMinistryName ||
                     i.Project.Ministry.MinistryDisplayName_EN == userMinistryName ||
                     i.Project.Ministry.MinistryUserName == userMinistryName));
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                indicators = indicators.Where(i =>
                    EF.Functions.Like(i.Name, $"%{searchString}%") ||
                    (i.SubOutput != null && EF.Functions.Like(i.SubOutput.Name, $"%{searchString}%")) ||
                    (i.Project != null && EF.Functions.Like(i.Project.ProjectName, $"%{searchString}%")));
            }

            var resultIndicators = await indicators.ToListAsync();

            // Pass suboutput name to view if viewing specific suboutput
            if (subOutputCode.HasValue && resultIndicators.Any())
            {
                ViewBag.SubOutputName = resultIndicators.First().SubOutput?.Name;
            }

            // Pass SubOutputs for the create form when accessed from framework level
            if (frameworkCode.HasValue && !subOutputCode.HasValue)
            {
                ViewBag.SubOutputsForCreate = await _context.SubOutputs
                    .Include(so => so.Output)
                    .ThenInclude(o => o.Outcome)
                    .Where(so => so.Output.Outcome.FrameworkCode == frameworkCode.Value)
                    .OrderBy(so => so.Name)
                    .ToListAsync();
            }

            return View(resultIndicators);
        }





        // GET: Indicators/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var indicator = await _context.Indicators
        //        .Include(i => i.SubOutput)
        //        .FirstOrDefaultAsync(m => m.Code == id);
        //    if (indicator == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(indicator);
        //}

        // GET: Indicators/Create
        [Permission(Permissions.AddIndicator)]
        public IActionResult Create(int? id)
        {
            // Populate dropdown only if no SubOutput is preselected
            ViewData["SubOutputCode"] = id == null
                ? new SelectList(_context.SubOutputs, "Code", "Name")
                : new SelectList(_context.SubOutputs, "Code", "Name", id);

            // Pass the selected framework code to the view
            ViewBag.SelectedSubOutputCode = id;

            return View();
        }

        // POST: Indicators/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> Create(Indicator indicator)
        {
            ModelState.Remove(nameof(indicator.SubOutput));

            // Check if indicator name already exists within the same suboutput
            var existingIndicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.SubOutputCode == indicator.SubOutputCode &&
                                          i.Name.ToLower() == indicator.Name.Trim().ToLower());
            if (existingIndicator != null)
            {
                ModelState.AddModelError("Name", _localizer["An indicator with this name already exists in this suboutput."]);
                ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
                return View(indicator);
            }

            //To Fix
            if (ModelState.IsValid || true)
            {
                // Add the new indicator
                _context.Add(indicator);
                await _context.SaveChangesAsync();

                // Update related entities
                await UpdateSubOutputPerformance(indicator.SubOutputCode);
                // Recalculate weights
                await RedistributeWeights(indicator.SubOutputCode);

                return RedirectToAction(nameof(Index), new { frameworkCode = indicator.SubOutput.Output.Outcome.FrameworkCode, subOutputCode = indicator.SubOutputCode });
            }

            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
            return View(indicator);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> CreateInline(string Name, int Target, int SubOutputCode)
        {
            if (string.IsNullOrWhiteSpace(Name) || Target <= 0)
            {
                TempData["Error"] = _localizer["Name and Target are required and must be valid."].Value;
                return RedirectToAction("Index", new { subOutputCode = SubOutputCode });
            }

            // Check if indicator name already exists within the same suboutput
            var existingIndicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.SubOutputCode == SubOutputCode &&
                                          i.Name.ToLower() == Name.Trim().ToLower());
            if (existingIndicator != null)
            {
                TempData["Error"] = _localizer["An indicator with this name already exists in this suboutput."].Value;
                return RedirectToAction("Index", new { subOutputCode = SubOutputCode });
            }

            var indicator = new Indicator
            {
                Name = Name,
                Target = Target,
                SubOutputCode = SubOutputCode
            };

            _context.Indicators.Add(indicator);
            await _context.SaveChangesAsync();


            // Update related entities
            await UpdateSubOutputPerformance(indicator.SubOutputCode);
            // Recalculate weights
            await RedistributeWeights(indicator.SubOutputCode);

            return RedirectToAction(nameof(Index), new { frameworkCode = indicator.SubOutput.Output.Outcome.FrameworkCode, subOutputCode = indicator.SubOutputCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> CreateInlineAjax(string name, int subOutputCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = _localizer["Indicator name is required."].Value });
                }

                // Check if indicator name already exists within the same suboutput
                var existingIndicator = await _context.Indicators
                    .FirstOrDefaultAsync(i => i.SubOutputCode == subOutputCode &&
                                              i.Name.ToLower() == name.Trim().ToLower());
                if (existingIndicator != null)
                {
                    return Json(new { success = false, message = _localizer["An indicator with this name already exists in this suboutput."].Value });
                }

                var indicator = new Indicator
                {
                    Name = name.Trim(),
                    Target = 0,
                    SubOutputCode = subOutputCode,
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0,
                    FieldMonitoring = 0,
                    ImpactAssessment = 0
                };

                _context.Indicators.Add(indicator);
                await _context.SaveChangesAsync();

                // Update related entities
                await UpdateSubOutputPerformance(indicator.SubOutputCode);
                // Recalculate weights
                await RedistributeWeights(indicator.SubOutputCode);

                return Json(new
                {
                    success = true,
                    indicator = new
                    {
                        code = indicator.IndicatorCode,
                        name = indicator.Name,
                        weight = Math.Round(indicator.Weight, 2),
                        indicatorsPerformance = Math.Round(indicator.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(indicator.DisbursementPerformance, 2)
                    },
                    message = _localizer["Indicator created successfully!"].Value
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = _localizer["Could not create indicator. Please try again."].Value });
            }
        }

        /// <summary>
        /// This is the NEW action that handles the "Add & Create Project" button.
        /// It creates the Indicator and then redirects to the Create action in the ProjectsController,
        /// passing the new Indicator's ID and Name for auto-filling the project form.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> CreateAndRedirectToProject(string Name, int Target, int SubOutputCode)
        {
            if (string.IsNullOrWhiteSpace(Name) || Target <= 0)
            {
                TempData["Error"] = _localizer["Name and Target are required and must be valid."].Value;
                return RedirectToAction("Index", new { subOutputCode = SubOutputCode });
            }

            // Check if indicator name already exists within the same suboutput
            var existingIndicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.SubOutputCode == SubOutputCode &&
                                          i.Name.ToLower() == Name.Trim().ToLower());
            if (existingIndicator != null)
            {
                TempData["Error"] = _localizer["An indicator with this name already exists in this suboutput."].Value;
                return RedirectToAction("Index", new { subOutputCode = SubOutputCode });
            }

            var indicator = new Indicator
            {
                Name = Name,
                Target = Target,
                SubOutputCode = SubOutputCode
            };

            _context.Indicators.Add(indicator);
            await _context.SaveChangesAsync(); // This saves the indicator and populates its ID

            // Update related entities
            await UpdateSubOutputPerformance(indicator.SubOutputCode);
            // Recalculate weights
            await RedistributeWeights(indicator.SubOutputCode);

            TempData["Success"] = _localizer["Indicator created. You can now add project details."].Value;

            // Redirect to the "Create" action in the "Projects" controller.
            // Pass the newly created indicator's ID and Name so the project can be associated with it
            // and the project name can be auto-filled.
            return RedirectToAction("Create", "Projects", new { indicatorId = indicator.IndicatorCode, indicatorName = indicator.Name });
        }

        // تحديث SubOutput بناءً على Indicators
        public async Task UpdateSubOutputPerformance(int subOutputCode)
        {
            var subOutput = await _context.SubOutputs
                .Include(s => s.Indicators)
                .FirstOrDefaultAsync(s => s.Code == subOutputCode);

            if (subOutput == null)
                throw new Exception("SubOutput not found");

            double totalWeight = subOutput.Indicators.Sum(i => i.Weight);

            if (totalWeight <= 0) totalWeight = subOutput.Indicators.Count; // fallback للأوزان المتساوية

            double weightedPerformance = subOutput.Indicators.Sum(i => i.IndicatorsPerformance * i.Weight / totalWeight);

            subOutput.IndicatorsPerformance = Math.Round(weightedPerformance, 2);

            _context.SubOutputs.Update(subOutput);
            await _context.SaveChangesAsync();

            await UpdateOutputPerformance(subOutput.OutputCode);
        }

        // تحديث Output بناءً على SubOutputs
        public async Task UpdateOutputPerformance(int outputCode)
        {
            var output = await _context.Outputs
                .Include(o => o.SubOutputs)
                .FirstOrDefaultAsync(o => o.Code == outputCode);

            if (output == null)
                throw new Exception("Output not found");

            double totalWeight = output.SubOutputs.Sum(s => s.Weight);

            if (totalWeight <= 0) totalWeight = output.SubOutputs.Count;

            double weightedPerformance = output.SubOutputs.Sum(s => s.IndicatorsPerformance * s.Weight / totalWeight);

            output.IndicatorsPerformance = Math.Round(weightedPerformance, 2);

            _context.Outputs.Update(output);
            await _context.SaveChangesAsync();

            await UpdateOutcomePerformance(output.OutcomeCode);
        }

        // تحديث Outcome بناءً على Outputs
        public async Task UpdateOutcomePerformance(int outcomeCode)
        {
            var outcome = await _context.Outcomes
                .Include(o => o.Outputs)
                .FirstOrDefaultAsync(o => o.Code == outcomeCode);

            if (outcome == null)
                throw new Exception("Outcome not found");

            double totalWeight = outcome.Outputs.Sum(o => o.Weight);

            if (totalWeight <= 0) totalWeight = outcome.Outputs.Count;

            double weightedPerformance = outcome.Outputs.Sum(o => o.IndicatorsPerformance * o.Weight / totalWeight);

            outcome.IndicatorsPerformance = Math.Round(weightedPerformance, 2);

            _context.Outcomes.Update(outcome);
            await _context.SaveChangesAsync();

            await UpdateFrameworkPerformance(outcome.FrameworkCode);
        }

        // تحديث Framework بناءً على Outcomes
        public async Task UpdateFrameworkPerformance(int frameworkCode)
        {
            var framework = await _context.Frameworks
                .Include(f => f.Outcomes)
                .FirstOrDefaultAsync(f => f.Code == frameworkCode);

            if (framework == null)
                throw new Exception("Framework not found");

            double totalWeight = framework.Outcomes.Sum(o => o.Weight);

            if (totalWeight <= 0) totalWeight = framework.Outcomes.Count;

            double weightedPerformance = framework.Outcomes.Sum(o => o.IndicatorsPerformance * o.Weight / totalWeight);

            framework.IndicatorsPerformance = Math.Round(weightedPerformance, 2);

            _context.Frameworks.Update(framework);
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> InlineEditName(int id, [FromBody] JsonElement data)
        {
            var indicator = await _context.Indicators.FindAsync(id);
            if (indicator == null) return NotFound();

            var newName = data.GetProperty("name").GetString();
            indicator.Name = newName;
            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpPost]
        [Permission(Permissions.DeleteIndicator)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var indicator = await _context.Indicators.FindAsync(id);
            if (indicator == null)
            {
                return NotFound();
            }

            if (indicator.ProjectID.HasValue)
            {
                var monitoringService = new MonitoringService(_context);
                await monitoringService.DeleteProjectAndRecalculateAsync(indicator.ProjectID.Value);
            }
            else
            {
                _context.Indicators.Remove(indicator);
                await _context.SaveChangesAsync();
            }

            await RedistributeWeights(indicator.SubOutputCode);
            await _planService.RecalculatePerformanceAfterIndicatorDeletion(indicator);

            return Ok();
        }



        // GET: Indicators/Edit/5
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indicator = await _context.Indicators.FindAsync(id);
            if (indicator == null)
            {
                return NotFound();
            }
            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
            if (indicator.ProjectID.HasValue)
            {
                var project = await _context.Projects.FindAsync(indicator.ProjectID.Value);
                ViewBag.ProjectName = project?.ProjectName;
            }
            return View(indicator);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> Edit(int id,Indicator indicator)
        {
            if (id != indicator.IndicatorCode)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(indicator.SubOutput));
            ModelState.Remove(nameof(indicator.Project));
            ModelState.Remove(nameof(indicator.ProjectID));

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Indicators.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.Name = indicator.Name;
                    existing.SubOutputCode = indicator.SubOutputCode;
                    // ProjectID is immutable — not overwritten

                    await _context.SaveChangesAsync();

                    // Update related entities
                    await UpdateSubOutputPerformance(existing.SubOutputCode);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IndicatorExists(indicator.IndicatorCode))
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

            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
            if (indicator.ProjectID.HasValue)
            {
                var project = await _context.Projects.FindAsync(indicator.ProjectID.Value);
                ViewBag.ProjectName = project?.ProjectName;
            }
            return View(indicator);
        }

        // GET: Indicators/Delete/5
        [Permission(Permissions.DeleteIndicator)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indicator = await _context.Indicators
                .Include(i => i.SubOutput)
                .FirstOrDefaultAsync(m => m.IndicatorCode == id);
            if (indicator == null)
            {
                return NotFound();
            }

            return View(indicator);
        }
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Details(int id)
        {
            var indicator = await _context.Indicators
                .Include(i => i.SubOutput)
                .ThenInclude(so => so.Output)
                .ThenInclude(o => o.Outcome)
                .ThenInclude(oc => oc.Framework)
                .FirstOrDefaultAsync(i => i.IndicatorCode == id);

            if (indicator == null)
                return NotFound();

            // Build the hierarchy model
            var hierarchy = new List<(string Name, string Type, int Code)>
    {
        (indicator.SubOutput.Output.Outcome.Framework.Name, "Framework", indicator.SubOutput.Output.Outcome.Framework.Code),
        (indicator.SubOutput.Output.Outcome.Name, "Outcome", indicator.SubOutput.Output.Outcome.Code),
        (indicator.SubOutput.Output.Name, "Output", indicator.SubOutput.Output.Code),
        (indicator.SubOutput.Name, "SubOutput", indicator.SubOutput.Code),
        (indicator.Name, "Indicator", indicator.IndicatorCode)
    };

            // Measures are now linked to ProjectPhases; get them via the indicator's project
            var measures = indicator.ProjectID.HasValue
                ? await _context.Measures
                    .Include(m => m.ProjectPhase)
                    .Where(m => m.ProjectPhase.ProjectID == indicator.ProjectID.Value)
                    .OrderBy(m => m.Date)
                    .ToListAsync()
                : new List<Measure>();

            var labels = new List<string>();
            var realData = new List<double>();
            var historicalData = new List<double>();
            var requiredData = new List<double>();

            foreach (var measure in measures)
            {
                labels.Add(measure.Date.ToString());
                realData.Add(measure.Value);
                historicalData.Add(measure.Value + 20);
                requiredData.Add(measure.Value + 10);
            }

            var chartDataViewModel = new ChartDataViewModel
            {
                Labels = labels,
                RealData = realData,
                HistoricalData = historicalData,
                RequiredData = requiredData
            };

            var model = new IndicatorDetailsViewModel
            {
                Indicator = indicator,
                Hierarchy = hierarchy,
                Measures = measures,
                ChartDataViewModel = chartDataViewModel
            };

            return View(model);
        }


        [HttpGet]
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> GetMeasureChartData(int indicatorCode)
        {
            // Get indicator to find its project
            var indicatorForChart = await _context.Indicators
                .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorCode);

            var data = indicatorForChart?.ProjectID.HasValue == true
                ? await _context.Measures
                    .Where(m => m.ProjectPhase.ProjectID == indicatorForChart.ProjectID.Value)
                    .OrderBy(m => m.Date)
                    .Select(m => new { date = m.Date.ToString("yyyy-MM-dd"), value = m.Value })
                    .ToListAsync()
                : new List<object>().Select(x => new { date = "", value = 0.0 }).ToList();

            var targetValue = indicatorForChart?.Target ?? 0;
            var target = new[] { new { date = "baseline", value = targetValue } };

            var result = new { Real = data, Target = target };

            return Json(result);
        }

        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> MeasureTablePartial(int indicatorCode)
        {
            var indicatorForTable = await _context.Indicators
                .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorCode);

            var measures = indicatorForTable?.ProjectID.HasValue == true
                ? await _context.Measures
                    .Where(m => m.ProjectPhase.ProjectID == indicatorForTable.ProjectID.Value)
                    .OrderBy(m => m.Date)
                    .ToListAsync()
                : new List<Measure>();

            return PartialView("_MeasureTablePartial", measures);
        }

   
        private bool IndicatorExists(int id)
        {
            return _context.Indicators.Any(e => e.IndicatorCode == id);
        }

        [Permission(Permissions.ReadIndicators)]
        public IActionResult Chart()
        {
            var viewModel = new ChartDataViewModel
            {
                Labels = new List<string>
            {
                "2019-01-01", "2022-01-01", "2030-02-01", "2030-03-01",
                "2030-04-01", "2030-05-01", "2030-06-01"
            },
                RealData = new List<double> { 80, 85, 90, 95, 98, 99, 100 },
                HistoricalData = new List<double> { 80, 82, 83, 85, 87, 89, 91 },
                RequiredData = new List<double> { 80, 83, 86, 89, 92, 95, 100 }
            };

            return View(viewModel);
        }
        private async Task RedistributeWeights(int subOutputCode)
        {
            var indicators = await _context.Indicators
                .Where(i => i.SubOutputCode == subOutputCode)
                .ToListAsync();

            if (indicators.Count == 0)
                return;

            double equalWeight = 100.0 / indicators.Count;

            foreach (var i in indicators)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = indicators.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                indicators.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

       // GET: Indicators/AdjustWeights/5
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> AdjustWeights(int frameworkCode, int subOutputCode)
        {
            var indicators = await _context.Indicators
                .Where(i => i.SubOutputCode == subOutputCode)
                .ToListAsync();

            var model = indicators.Select(i => new IndicatorViewModel
            {
                Code = i.IndicatorCode,
                Name = i.Name,
                Weight = i.Weight
            }).ToList();

            ViewBag.SubOutputCode = subOutputCode;
            ViewBag.FrameworkCode = frameworkCode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> AdjustWeights(List<IndicatorViewModel> model, int frameworkCode, int subOutputCode)
        {
            double totalWeight = model.Sum(i => i.Weight);

            if (Math.Abs(totalWeight - 100.0) > 0.01)
            {
                ModelState.AddModelError("", _localizer["Total weight must equal 100%."]);
                ViewBag.SubOutputCode = subOutputCode;
                ViewBag.FrameworkCode = frameworkCode;
                return View(model);
            }

            foreach (var vm in model)
            {
                var indicator = await _context.Indicators.FindAsync(vm.Code);
                if (indicator != null)
                {
                    indicator.Weight = vm.Weight;
                    _context.Update(indicator);
                }
            }

            await _context.SaveChangesAsync();

            // بعد حفظ الأوزان الجديدة، إعادة حساب أداء SubOutput بناء على الأوزان
            await UpdateSubOutputPerformance(subOutputCode);

            return RedirectToAction(nameof(Index), new { frameworkCode = frameworkCode, subOutputCode = subOutputCode });
        }

        // GET: Indicators/IndicatorAndProject
        [Permission(Permissions.ReadIndicators)]
        public IActionResult IndicatorAndProject(int? projectId, int? frameworkCode, int? subOutputCode, string searchString)
        {
            // Merged into Index — redirect to preserve any bookmarked/linked URLs
            return RedirectToAction("Index", new { frameworkCode, subOutputCode, searchString, projectId });
        }

        // GET: Demo page for project display options
        public IActionResult ProjectDisplayOptions()
        {
            return View();
        }

        // GET: Indicators/IndicatorAndProjectTable
        [Permission(Permissions.ReadIndicators)]
        public IActionResult IndicatorAndProjectTable(int? projectId, int? frameworkCode, int? subOutputCode, string searchString)
        {
            // Merged into Index — redirect to preserve any bookmarked/linked URLs
            return RedirectToAction("Index", new { frameworkCode, subOutputCode, searchString, projectId });
        }

        // GET: Indicators/ExportExcel
        [HttpGet]
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> ExportExcel(int? frameworkCode, int? subOutputCode)
        {
            var indicators = await GetFilteredIndicators(frameworkCode, subOutputCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["Indicators"].Value);

            if (isRtl) worksheet.RightToLeft = true;

            worksheet.Cell(1, 1).Value = _localizer["Indicator Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Weight"].Value + " (%)";
            worksheet.Cell(1, 3).Value = _localizer["Target"].Value;
            worksheet.Cell(1, 4).Value = _localizer["Indicators Performance"].Value + " (%)";
            worksheet.Cell(1, 5).Value = _localizer["SubOutput"].Value;

            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var indicator in indicators)
            {
                worksheet.Cell(row, 1).Value = indicator.Name;
                worksheet.Cell(row, 2).Value = Math.Round(indicator.Weight, 2);
                worksheet.Cell(row, 3).Value = indicator.Target;
                worksheet.Cell(row, 4).Value = Math.Round(indicator.IndicatorsPerformance, 2);
                worksheet.Cell(row, 5).Value = indicator.SubOutput?.Name ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            var dataRange = worksheet.Range(1, 1, row - 1, 5);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "المؤشرات" : "Indicators";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Indicators/ExportPdf
        [HttpGet]
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> ExportPdf(int? frameworkCode, int? subOutputCode)
        {
            var indicators = await GetFilteredIndicators(frameworkCode, subOutputCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    if (isRtl) page.ContentFromRightToLeft();

                    page.Header().PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(_localizer["Indicators"].Value).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                col.Item().Text($"{_localizer["Generated on"].Value}: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                        });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Indicator Name"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Weight"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Target"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Indicators Performance"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["SubOutput"].Value).FontColor(Colors.White).Bold();
                        });

                        foreach (var indicator in indicators)
                        {
                            var performance = Math.Round(indicator.IndicatorsPerformance, 2);

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(indicator.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{Math.Round(indicator.Weight, 2)}%");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(indicator.Target.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{performance}%").FontColor(GetPerformanceColor(performance));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(indicator.SubOutput?.Name ?? "");
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span(_localizer["Page"].Value + " ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var filePrefix = isRtl ? "المؤشرات" : "Indicators";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<Indicator>> GetFilteredIndicators(int? frameworkCode, int? subOutputCode)
        {
            var query = _context.Indicators.Include(i => i.SubOutput).AsQueryable();

            if (frameworkCode.HasValue)
                query = query.Where(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode.Value);

            if (subOutputCode.HasValue)
                query = query.Where(i => i.SubOutputCode == subOutputCode.Value);

            return await query.OrderByDescending(i => i.IndicatorsPerformance).ToListAsync();
        }

        private static string GetPerformanceColor(double performance)
        {
            return performance switch
            {
                >= 75 => Colors.Green.Darken2,
                >= 50 => Colors.Orange.Darken2,
                _ => Colors.Red.Darken2
            };
        }
    }


}

