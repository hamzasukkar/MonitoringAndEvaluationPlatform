using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class OutcomesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPerformanceService _performanceService;

        public OutcomesController(ApplicationDbContext context, IPerformanceService performanceService)
        {
            _context = context;
            _performanceService = performanceService;
        }

        // GET: Outcomes
        [Permission(Permissions.ReadOutcomes)]
        public async Task<IActionResult> Index(int? frameworkCode, string sortOrder)
        {
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.WeightSortParm = sortOrder == "weight" ? "weight_desc" : "weight";
            ViewBag.IndicatorsSortParm = String.IsNullOrEmpty(sortOrder) ? "indicators" : (sortOrder == "indicators" ? "indicators_desc" : "indicators");
            ViewBag.DisbursementSortParm = sortOrder == "disbursement" ? "disbursement_desc" : "disbursement";
            ViewBag.FrameworkSortParm = sortOrder == "framework" ? "framework_desc" : "framework";
            ViewBag.CurrentSort = sortOrder;

            IQueryable<Outcome> outcomesQuery;

            if (frameworkCode == null)
            {
                outcomesQuery = _context.Outcomes.Include(o => o.Framework);
            }
            else
            {
                ViewBag.SelectedFrameworkCode = frameworkCode;
                outcomesQuery = _context.Outcomes
                    .Include(o => o.Framework)
                    .Include(x => x.Outputs)
                    .Where(m => m.FrameworkCode == frameworkCode);
            }

            // Apply sorting
            outcomesQuery = sortOrder switch
            {
                "name" => outcomesQuery.OrderBy(o => o.Name),
                "name_desc" => outcomesQuery.OrderByDescending(o => o.Name),
                "weight" => outcomesQuery.OrderBy(o => o.Weight),
                "weight_desc" => outcomesQuery.OrderByDescending(o => o.Weight),
                "indicators" => outcomesQuery.OrderBy(o => o.IndicatorsPerformance),
                "indicators_desc" => outcomesQuery.OrderByDescending(o => o.IndicatorsPerformance),
                "disbursement" => outcomesQuery.OrderBy(o => o.DisbursementPerformance),
                "disbursement_desc" => outcomesQuery.OrderByDescending(o => o.DisbursementPerformance),
                "framework" => outcomesQuery.OrderBy(o => o.Framework.Name),
                "framework_desc" => outcomesQuery.OrderByDescending(o => o.Framework.Name),
                _ => outcomesQuery.OrderByDescending(o => o.IndicatorsPerformance) // Default: highest Indicators Performance first
            };

            var outcomes = await outcomesQuery.ToListAsync();

            if (outcomes == null)
            {
                return NotFound();
            }

            return View(outcomes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddOutcome)]
        public async Task<IActionResult> CreateInline(string name, int frameworkCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Outcome name is required." });
                }

                var outcome = new Outcome
                {
                    Name = name.Trim(),
                    FrameworkCode = frameworkCode,
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0,
                    FieldMonitoring = 0,
                    ImpactAssessment = 0
                };

                _context.Add(outcome);
                await _context.SaveChangesAsync();

                // Recalculate weights
                await RedistributeWeights(frameworkCode);

                return Json(new
                {
                    success = true,
                    outcome = new
                    {
                        code = outcome.Code,
                        name = outcome.Name,
                        weight = Math.Round(outcome.Weight, 2),
                        indicatorsPerformance = Math.Round(outcome.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(outcome.DisbursementPerformance, 2),
                        frameworkName = outcome.Framework?.Name ?? ""
                    },
                    message = "Outcome created successfully!"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while creating the outcome." });
            }
        }

        [HttpPost]
        [Permission(Permissions.ModifyOutcome)]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var outcome = await _context.Outcomes.FindAsync(id);
            if (outcome == null) return NotFound();

            outcome.Name = name;
            _context.Update(outcome);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Outcomes/Delete/5
        [HttpPost]
        [Permission(Permissions.DeleteOutcome)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var outcome = await _context.Outcomes.FindAsync(id);
            if (outcome == null) return NotFound();

            _context.Outcomes.Remove(outcome);
            await _context.SaveChangesAsync();
            return Ok();
        }

        private bool OutcomeExists(int id)
        {
            return _context.Outcomes.Any(e => e.Code == id);
        }

        // GET: FramworkOutcomes
        [Permission(Permissions.ReadOutcomes)]
        public async Task<IActionResult> FramworkOutcomes(int? id)
        {
            var applicationDbContext = _context.Outcomes.Where(m => m.FrameworkCode == id);
            ViewData["FrameworkName"] = _context.Frameworks.Where(i => i.Code == id).FirstOrDefault().Name;
            return View(await applicationDbContext.ToListAsync());
        }

        private async Task RedistributeWeights(int frameworkCode)
        {
            var outcomes = await _context.Outcomes
                .Where(i => i.FrameworkCode == frameworkCode)
                .ToListAsync();

            if (outcomes.Count == 0)
                return;

            double equalWeight = 100.0 / outcomes.Count;

            foreach (var i in outcomes)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = outcomes.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                outcomes.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

        [Permission(Permissions.ModifyOutcome)]
        public async Task<IActionResult> AdjustWeights(int frameworkCode)
        {
            var outcomes = await _context.Outcomes
                .Where(i => i.FrameworkCode == frameworkCode)
                .ToListAsync();

            var model = outcomes.Select(i => new OutcomesViewModel
            {
                Code = i.Code,
                Name = i.Name,
                Weight = i.Weight
            }).ToList();

            ViewBag.FrameworkCode = frameworkCode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyOutcome)]
        public async Task<IActionResult> AdjustWeights(List<OutcomesViewModel> model,int frameworkCode)
        {
            double totalWeight = model.Sum(i => i.Weight);

            if (Math.Abs(totalWeight - 100.0) > 0.01)
            {
                ModelState.AddModelError("", "Total weight must equal 100%.");
                ViewBag.FrameworkCode = frameworkCode;
                return View(model);
            }

            foreach (var vm in model)
            {
                var outcome = await _context.Outcomes.FindAsync(vm.Code);
                if (outcome != null)
                {
                    outcome.Weight = vm.Weight;
                    _context.Update(outcome);
                }
            }

            await _context.SaveChangesAsync();

            await _performanceService.UpdateFrameworkPerformance(frameworkCode);

            return RedirectToAction(nameof(Index), new { frameworkCode = frameworkCode });
        }
    }
}

