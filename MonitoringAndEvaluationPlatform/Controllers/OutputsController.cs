using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize(Roles = "Admin")]
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

            return View(outputs);
        }

        // GET: Outputs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var output = await _context.Outputs
                .Include(o => o.Outcome)
                .Include(o => o.SubOutputs)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (output == null)
            {
                return NotFound();
            }

            return View(output);
        }

        // GET: Outputs/Create
        public IActionResult Create(int? outcomeCode,int? frameworkCode)
        {
            ViewData["OutcomeCode"] = new SelectList(_context.Outcomes, "Code", "Name");

            var outcomes = _context.Outcomes.ToList();

            // Populate dropdown only if no framework is preselected
            ViewData["OutcomeCode"] = outcomeCode == null
                ? new SelectList(outcomes, "Code", "Name")
                : new SelectList(outcomes, "Code", "Name", outcomeCode);

            // Pass the selected framework code to the view
            ViewBag.SelectedOutcomeCode = outcomeCode;
            ViewBag.SelectedFrameworkCode = frameworkCode;

            return View();
        }

        // POST: Outputs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,OutcomeCode")] Output output)
        {
            ModelState.Remove(nameof(output.Outcome));

            if (ModelState.IsValid)
            {
                _context.Add(output);
                await _context.SaveChangesAsync();
                await RedistributeWeights(output.OutcomeCode);
                return RedirectToAction("Index", new { outcomeCode = output.OutcomeCode });
            }
            ViewData["OutcomeCode"] = new SelectList(_context.Outcomes, "Code", "Name", output.OutcomeCode);
            return View(output);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var output = await _context.Outputs.FindAsync(id);
            if (output == null) return NotFound();

            output.Name = name;
            _context.Update(output);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: Outputs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var output = await _context.Outputs.FindAsync(id);
            if (output == null)
            {
                return NotFound();
            }
            ViewData["OutcomeCode"] = new SelectList(_context.Outcomes, "Code", "Name", output.OutcomeCode);
            return View(output);
        }

        // POST: Outputs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment,OutcomeCode")] Output output)
        {
            if (id != output.Code)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(output.Outcome));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(output);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OutputExists(output.Code))
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
            ViewData["OutcomeCode"] = new SelectList(_context.Outcomes, "Code", "Name", output.OutcomeCode);
            return View(output);
        }

        // GET: Outputs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var output = await _context.Outputs
                .Include(o => o.Outcome)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (output == null)
            {
                return NotFound();
            }

            return View(output);
        }

        [HttpPost]
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

