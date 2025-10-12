using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class OutputsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPerformanceService _performanceService;

        public OutputsController(ApplicationDbContext context, IPerformanceService performanceService)
        {
            _context = context;
            _performanceService = performanceService;
        }

        // GET: Outputs
        [Permission(Permissions.ReadOutputs)]
        public async Task<IActionResult> Index(int? frameworkCode, int? outcomeCode)
        {
            // Start with base query including all related entities
            var query = _context.Outputs
                .Include(o => o.Outcome)
                .Include(o => o.SubOutputs)
                .AsQueryable();

            // Apply framework filter if frameworkCode is provided
            if (frameworkCode.HasValue)
            {
                query = query.Where(o => o.Outcome.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            // Apply outcome filter if outcomeCode is provided
            if (outcomeCode.HasValue)
            {
                query = query.Where(o => o.OutcomeCode == outcomeCode.Value);
                ViewBag.SelectedOutcomeCode = outcomeCode.Value;
            }

            // Execute the query
            var outputs = await query.ToListAsync();

            // Set ViewData for breadcrumb
            ViewData["frameworkCode"] = frameworkCode;
            ViewData["outcomeCode"] = outcomeCode;

            return View(outputs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddOutput)]
        public async Task<IActionResult> CreateInline(string name, int outcomeCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Output name is required." });
                }

                var output = new Output
                {
                    Name = name.Trim(),
                    OutcomeCode = outcomeCode,
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0,
                    FieldMonitoring = 0,
                    ImpactAssessment = 0
                };

                _context.Add(output);
                await _context.SaveChangesAsync();

                // Recalculate weights
                await RedistributeWeights(outcomeCode);

                return Json(new
                {
                    success = true,
                    output = new
                    {
                        code = output.Code,
                        name = output.Name,
                        weight = Math.Round(output.Weight, 2),
                        indicatorsPerformance = Math.Round(output.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(output.DisbursementPerformance, 2),
                        outcomeName = output.Outcome?.Name ?? ""
                    },
                    message = "Output created successfully!"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while creating the output." });
            }
        }

        [HttpPost]
        [Permission(Permissions.ModifyOutput)]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var output = await _context.Outputs.FindAsync(id);
            if (output == null) return NotFound();

            output.Name = name;
            _context.Update(output);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Permission(Permissions.DeleteOutput)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var output = await _context.Outputs.FindAsync(id);
            if (output == null) return NotFound();

            _context.Outputs.Remove(output);
            await _context.SaveChangesAsync();
            return Ok();
        }

        private bool OutputExists(int id)
        {
            return _context.Outputs.Any(e => e.Code == id);
        }

        private async Task RedistributeWeights(int outcomeCode)
        {
            var outputs = await _context.Outputs
                .Where(i => i.OutcomeCode == outcomeCode)
                .ToListAsync();

            if (outputs.Count == 0)
                return;

            double equalWeight = 100.0 / outputs.Count;

            foreach (var i in outputs)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = outputs.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                outputs.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

        // GET: Indicators/AdjustWeights/5
        [Permission(Permissions.ModifyOutput)]
        public async Task<IActionResult> AdjustWeights(int outcomeCode)
        {
            var outputs = await _context.Outputs
                .Where(i => i.OutcomeCode == outcomeCode)
                .ToListAsync();

            var model = outputs.Select(i => new OutputViewModel
            {
                Code = i.Code,
                Name = i.Name,
                Weight = i.Weight
            }).ToList();

            ViewBag.OutcomeCode = outcomeCode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyOutput)]
        public async Task<IActionResult> AdjustWeights(List<OutputViewModel> model, int outcomeCode)
        {
            double totalWeight = model.Sum(i => i.Weight);

            if (Math.Abs(totalWeight - 100.0) > 0.01)
            {
                ModelState.AddModelError("", "Total weight must equal 100%.");
                ViewBag.OutcomeCode = outcomeCode;
                return View(model);
            }

            foreach (var vm in model)
            {
                var output = await _context.Outputs.FindAsync(vm.Code);
                if (output != null)
                {
                    output.Weight = vm.Weight;
                    _context.Update(output);
                }
            }

            await _context.SaveChangesAsync();

            await _performanceService.UpdateOutcomePerformance(outcomeCode);

            return RedirectToAction(nameof(Index), new { outcomeCode = outcomeCode });
        }
    }
}

