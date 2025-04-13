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
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OutcomesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OutcomesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Outcomes
        public async Task<IActionResult> Index(int? frameworkCode)
        {
            if (frameworkCode == null)
            {
                var applicationDbContext = _context.Outcomes.Include(o => o.Framework);
                return View(await applicationDbContext.ToListAsync());
            }

            ViewBag.SelectedFrameworkCode = frameworkCode; // Store it for use in the view

            var outcomes = await _context.Outcomes
              .Include(o => o.Framework).Include(x => x.Outputs)
              .Where(m => m.FrameworkCode==frameworkCode).ToListAsync();

            if (outcomes == null)
            {
                return NotFound();
            }

            return View(outcomes);
        }

        // GET: Outcomes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outcome = await _context.Outcomes
                .Include(o => o.Framework).Include(x => x.Outputs)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (outcome == null)
            {
                return NotFound();
            }

            return View(outcome);
        }

        // GET: Outcomes/Create
        public IActionResult Create(int? frameworkCode)
        {
            var frameworks = _context.Frameworks.ToList();

            // Populate dropdown only if no framework is preselected
            ViewData["FrameworkCode"] = frameworkCode == null
                ? new SelectList(frameworks, "Code", "Name")
                : new SelectList(frameworks, "Code", "Name", frameworkCode);

            // Pass the selected framework code to the view
            ViewBag.SelectedFrameworkCode = frameworkCode;

            return View();
        }



        // POST: Outcomes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Outcome outcome)
        {
            ModelState.Remove(nameof(outcome.Framework));


            if (ModelState.IsValid)
            {
                _context.Add(outcome);
                await _context.SaveChangesAsync();

                // Recalculate weights
                await RedistributeWeights(outcome.FrameworkCode);

                // Redirect to Index with FrameworkCode after creating the Outcome
                return RedirectToAction("Index", new { frameworkCode = outcome.FrameworkCode });
            }

            // If ModelState is invalid, reload Framework dropdown
            ViewData["FrameworkCode"] = new SelectList(_context.Frameworks, "Code", "Name", outcome.FrameworkCode);
            return View(outcome);
        }


        // GET: Outcomes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outcome = await _context.Outcomes.FindAsync(id);
            if (outcome == null)
            {
                return NotFound();
            }
            ViewData["FrameworkCode"] = new SelectList(_context.Frameworks, "Code", "Name", outcome.FrameworkCode);
            return View(outcome);
        }

        // POST: Outcomes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment,FrameworkCode")] Outcome outcome)
        {
            if (id != outcome.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid ||true)
            {
                try
                {
                    _context.Update(outcome);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OutcomeExists(outcome.Code))
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
            ViewData["FrameworkCode"] = new SelectList(_context.Frameworks, "Code", "Name", outcome.FrameworkCode);
            return View(outcome);
        }

        // GET: Outcomes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outcome = await _context.Outcomes
                .Include(o => o.Framework)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (outcome == null)
            {
                return NotFound();
            }

            return View(outcome);
        }

        // POST: Outcomes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var outcome = await _context.Outcomes.FindAsync(id);
            if (outcome != null)
            {
                _context.Outcomes.Remove(outcome);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OutcomeExists(int id)
        {
            return _context.Outcomes.Any(e => e.Code == id);
        }

        // GET: FramworkOutcomes
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
            return RedirectToAction(nameof(Index), new { frameworkCode = frameworkCode });
        }
    }
}

