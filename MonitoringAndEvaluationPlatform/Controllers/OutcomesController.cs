using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class OutcomesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OutcomesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Outcomes
        public async Task<IActionResult> Index(int? id)
        {
            if (id == null)
            {
                var applicationDbContext = _context.Outcomes.Include(o => o.Framework);
                return View(await applicationDbContext.ToListAsync());
            }
            var outcomes = await _context.Outcomes
              .Include(o => o.Framework).Include(x => x.Outputs)
              .Where(m => m.FrameworkCode==id).ToListAsync();

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
        public IActionResult Create()
        {
            ViewData["FrameworkCode"] = new SelectList(_context.Framework, "Code", "Name");
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["FrameworkCode"] = new SelectList(_context.Framework, "Code", "Name", outcome.FrameworkCode);

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
            ViewData["FrameworkCode"] = new SelectList(_context.Framework, "Code", "Name", outcome.FrameworkCode);
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
            ViewData["FrameworkCode"] = new SelectList(_context.Framework, "Code", "Name", outcome.FrameworkCode);
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
            ViewData["FrameworkName"] = _context.Framework.Where(i => i.Code == id).FirstOrDefault().Name;
            return View(await applicationDbContext.ToListAsync());
        }
    }
}
