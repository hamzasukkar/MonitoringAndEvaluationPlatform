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
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class IndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PlanService _planService;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndicatorsController(ApplicationDbContext context, PlanService planService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _planService = planService;
            _userManager = userManager;
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

            if (frameworkCode == null)
            {
                // Include the SubOutput navigation property
                var indicators = _context.Indicators
                    .Include(i => i.SubOutput)
                    .Include(i => i.ProjectIndicators)
                        .ThenInclude(pi => pi.Project)
                            .ThenInclude(p => p.Ministry)
                    .AsQueryable();

                // Filter by ministry if user is a ministry user
                if (isMinistryUser && !string.IsNullOrEmpty(userMinistryName))
                {
                    indicators = indicators.Where(i =>
                        i.ProjectIndicators.Any(pi => pi.Project.Ministry != null && (pi.Project.Ministry.MinistryDisplayName_AR == userMinistryName || pi.Project.Ministry.MinistryDisplayName_EN == userMinistryName || pi.Project.Ministry.MinistryUserName == userMinistryName)));
                }

                // Filter results if searchString is provided
                if (!string.IsNullOrEmpty(searchString))
                {
                    indicators = indicators.Where(i => i.Name.Contains(searchString) ||
                                                     (i.SubOutput != null && i.SubOutput.Name.Contains(searchString)));
                }

                return View(await indicators.ToListAsync());
            }

            var frameworkIndicators = _context.Indicators
                .Where(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode)
                .Include(i => i.SubOutput)
                .Include(i => i.ProjectIndicators)
                    .ThenInclude(pi => pi.Project)
                        .ThenInclude(p => p.Ministry)
                .AsQueryable();

            // Filter by ministry if user is a ministry user
            if (isMinistryUser && !string.IsNullOrEmpty(userMinistryName))
            {
                frameworkIndicators = frameworkIndicators.Where(i =>
                    i.ProjectIndicators.Any(pi => pi.Project.Ministry != null && (pi.Project.Ministry.MinistryDisplayName_AR == userMinistryName || pi.Project.Ministry.MinistryDisplayName_EN == userMinistryName || pi.Project.Ministry.MinistryUserName == userMinistryName)));
            }

            // Add subOutputCode filter if it's provided
            if (subOutputCode != null)
            {
                frameworkIndicators = frameworkIndicators.Where(i => i.SubOutputCode== subOutputCode);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                frameworkIndicators = frameworkIndicators.Where(i => i.Name.Contains(searchString) ||
                                                   (i.SubOutput != null && i.SubOutput.Name.Contains(searchString)));
            }

            return View(await frameworkIndicators.ToListAsync());
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
                TempData["Error"] = "Name and Target are required and must be valid.";
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
        /// <summary>
        /// This is the NEW action that handles the "Add & Create Project" button.
        /// It creates the Indicator and then redirects to the Create action in the ProjectsController,
        /// passing the new Indicator's ID.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> CreateAndRedirectToProject(string Name, int Target, int SubOutputCode)
        {
            if (string.IsNullOrWhiteSpace(Name) || Target <= 0)
            {
                TempData["Error"] = "Name and Target are required and must be valid.";
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

            TempData["Success"] = "Indicator created. You can now add project details.";

            // Redirect to the "Create" action in the "Projects" controller.
            // Pass the newly created indicator's ID so the project can be associated with it.
            return RedirectToAction("Create", "Projects", new { indicatorId = indicator.IndicatorCode });
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

            _context.Indicators.Remove(indicator);
            await _context.SaveChangesAsync();
            await RedistributeWeights(indicator.SubOutputCode);
            await UpdateSubOutputPerformance(indicator.SubOutputCode);
            // Call recalculation BEFORE deleting the indicator
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

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the indicator
                    _context.Update(indicator);
                    await _context.SaveChangesAsync();

                    // Update related entities
                    await UpdateSubOutputPerformance(indicator.SubOutputCode);
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

            var measures = await _context.Measures.Where(m => m.IndicatorCode == id).ToListAsync();

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
            // Get indicator with all measures
            var indicator = await _context.Indicators
                .Include(i => i.Measures)
                .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorCode);

            if (indicator == null)
                return Json(new { real = new object[] { }, historical = new object[] { }, required = new object[] { } });

            // Real values - only Real ValueType measures
            var realMeasures = indicator.Measures
                .Where(m => m.ValueType == Enums.MeasureValueType.Real)
                .OrderBy(m => m.Date)
                .Select(m => new { date = m.Date.ToString("yyyy-MM-dd"), value = m.Value })
                .ToList();

            // Historical - cumulative of Real values
            var historical = new List<object>();
            double cumulative = 0;
            foreach (var measure in realMeasures)
            {
                cumulative += measure.value;
                historical.Add(new { date = measure.date, value = Math.Round(cumulative, 2) });
            }

            // Required - linear projection from baseline to target
            var required = new List<object>();
            if (realMeasures.Any() && indicator.Target > 0 && indicator.TargetYear.HasValue)
            {
                var startDate = realMeasures.First().date;
                var endDate = indicator.TargetYear.Value.ToString("yyyy-MM-dd");
                var baselineValue = realMeasures.First().value;
                var targetValue = indicator.Target;

                // Calculate required monthly growth
                var start = DateTime.Parse(startDate);
                var end = DateTime.Parse(endDate);
                var months = (end.Year - start.Year) * 12 + (end.Month - start.Month);

                if (months > 0)
                {
                    var increment = (targetValue - baselineValue) / (double)months;

                    for (int i = 0; i <= months; i++)
                    {
                        var date = start.AddMonths(i);
                        var value = baselineValue + (increment * i);
                        required.Add(new { date = date.ToString("yyyy-MM-dd"), value = Math.Round(value, 2) });
                    }
                }
            }

            var result = new { real = realMeasures, historical = historical, required = required };
            return Json(result);
        }

        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> MeasureTablePartial(int indicatorCode)
        {
            var measures = await _context.Measures
                .Where(m => m.IndicatorCode == indicatorCode)
                .OrderBy(m => m.Date)
                .ToListAsync();

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
                ModelState.AddModelError("", "Total weight must equal 100%.");
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
        public async Task<IActionResult> IndicatorAndProject(int? projectId, int? frameworkCode, int? subOutputCode, string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ProjectId"] = projectId;
            ViewData["FrameworkCode"] = frameworkCode;
            ViewData["SubOutputCode"] = subOutputCode;

            // Get projects filtered by frameworkCode if provided
            var projectsQuery = _context.Projects.AsQueryable();

            if (frameworkCode.HasValue)
            {
                // Filter projects that are associated with indicators belonging to the specified framework
                projectsQuery = projectsQuery.Where(p =>
                    p.ProjectIndicators.Any(pi =>
                        pi.Indicator.SubOutput.Output.Outcome.FrameworkCode == frameworkCode));
            }

            var projects = await projectsQuery
                .Select(p => new { p.ProjectID, p.ProjectName })
                .Distinct()
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            ViewData["Projects"] = new SelectList(projects, "ProjectID", "ProjectName", projectId);

            // Base query for indicators with their related projects
            var indicatorsQuery = _context.Indicators
                .Include(i => i.SubOutput)
                    .ThenInclude(so => so.Output)
                    .ThenInclude(o => o.Outcome)
                    .ThenInclude(oc => oc.Framework)
                .Include(i => i.ProjectIndicators)
                    .ThenInclude(pi => pi.Project)
                .AsQueryable();

            // Apply framework filter if provided
            if (frameworkCode.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode);
            }

            // Apply subOutput filter if provided
            if (subOutputCode.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.SubOutputCode == subOutputCode);
            }

            // Apply project filter if provided
            if (projectId.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.ProjectIndicators.Any(pi => pi.ProjectId == projectId));
            }

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                indicatorsQuery = indicatorsQuery.Where(i => 
                    i.Name.Contains(searchString) ||
                    (i.SubOutput != null && i.SubOutput.Name.Contains(searchString)) ||
                    i.ProjectIndicators.Any(pi => pi.Project.ProjectName.Contains(searchString)));
            }

            var indicators = await indicatorsQuery.ToListAsync();

            return View(indicators);
        }

        // GET: Demo page for project display options
        public IActionResult ProjectDisplayOptions()
        {
            return View();
        }

        // GET: Indicators/IndicatorAndProjectTable
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> IndicatorAndProjectTable(int? projectId, int? frameworkCode, int? subOutputCode, string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ProjectId"] = projectId;
            ViewData["frameworkCode"] = frameworkCode;
            ViewData["subOutputCode"] = subOutputCode;

            // Get projects filtered by frameworkCode if provided
            var projectsQuery = _context.Projects.AsQueryable();

            if (frameworkCode.HasValue)
            {
                // Filter projects that are associated with indicators belonging to the specified framework
                projectsQuery = projectsQuery.Where(p =>
                    p.ProjectIndicators.Any(pi =>
                        pi.Indicator.SubOutput.Output.Outcome.FrameworkCode == frameworkCode));
            }

            var projects = await projectsQuery
                .Select(p => new { p.ProjectID, p.ProjectName })
                .Distinct()
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            ViewData["Projects"] = new SelectList(projects, "ProjectID", "ProjectName", projectId);

            // Base query for indicators with their related projects
            var indicatorsQuery = _context.Indicators
                .Include(i => i.SubOutput)
                    .ThenInclude(so => so.Output)
                    .ThenInclude(o => o.Outcome)
                    .ThenInclude(oc => oc.Framework)
                .Include(i => i.ProjectIndicators)
                    .ThenInclude(pi => pi.Project)
                .AsQueryable();

            // Apply framework filter if provided
            if (frameworkCode.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode);
            }

            // Apply subOutput filter if provided
            if (subOutputCode.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.SubOutputCode == subOutputCode);
            }

            // Apply project filter if provided
            if (projectId.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.ProjectIndicators.Any(pi => pi.ProjectId == projectId));
            }

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                indicatorsQuery = indicatorsQuery.Where(i => 
                    i.Name.Contains(searchString) ||
                    (i.SubOutput != null && i.SubOutput.Name.Contains(searchString)) ||
                    i.ProjectIndicators.Any(pi => pi.Project.ProjectName.Contains(searchString)));
            }

            var indicators = await indicatorsQuery.ToListAsync();

            return View(indicators);
        }
    }


}

