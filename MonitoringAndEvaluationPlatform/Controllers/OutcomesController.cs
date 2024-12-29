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
        public async Task<IActionResult> Index()
        {
            return View(await _context.Outcomes.ToListAsync());
        }

        // GET: Outcomes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outcomes = await _context.Outcomes
                .FirstOrDefaultAsync(m => m.Code == id);
            if (outcomes == null)
            {
                return NotFound();
            }

            return View(outcomes);
        }

        // GET: Outcomes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Outcomes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Outcome outcomes)
        {
            if (ModelState.IsValid)
            {
                _context.Add(outcomes);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(outcomes);
        }

        // GET: Outcomes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outcomes = await _context.Outcomes.FindAsync(id);
            if (outcomes == null)
            {
                return NotFound();
            }
            return View(outcomes);
        }

        // POST: Outcomes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Outcome,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Outcome outcomes)
        {
            if (id != outcomes.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(outcomes);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OutcomesExists(outcomes.Code))
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
            return View(outcomes);
        }

        // GET: Outcomes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outcomes = await _context.Outcomes
                .FirstOrDefaultAsync(m => m.Code == id);
            if (outcomes == null)
            {
                return NotFound();
            }

            return View(outcomes);
        }

        // POST: Outcomes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var outcomes = await _context.Outcomes.FindAsync(id);
            if (outcomes != null)
            {
                _context.Outcomes.Remove(outcomes);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OutcomesExists(int id)
        {
            return _context.Outcomes.Any(e => e.Code == id);
        }
    }
}
