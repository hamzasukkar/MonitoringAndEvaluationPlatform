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
    public class OutputsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OutputsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Outputs
        public async Task<IActionResult> Index()
        {
            return View(await _context.Outputs.ToListAsync());
        }

        // GET: Outputs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outputs = await _context.Outputs
                .FirstOrDefaultAsync(m => m.Code == id);
            if (outputs == null)
            {
                return NotFound();
            }

            return View(outputs);
        }

        // GET: Outputs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Outputs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Output outputs)
        {
            if (ModelState.IsValid)
            {
                _context.Add(outputs);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(outputs);
        }

        // GET: Outputs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outputs = await _context.Outputs.FindAsync(id);
            if (outputs == null)
            {
                return NotFound();
            }
            return View(outputs);
        }

        // POST: Outputs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Output,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Output outputs)
        {
            if (id != outputs.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(outputs);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OutputsExists(outputs.Code))
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
            return View(outputs);
        }

        // GET: Outputs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outputs = await _context.Outputs
                .FirstOrDefaultAsync(m => m.Code == id);
            if (outputs == null)
            {
                return NotFound();
            }

            return View(outputs);
        }

        // POST: Outputs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var outputs = await _context.Outputs.FindAsync(id);
            if (outputs != null)
            {
                _context.Outputs.Remove(outputs);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OutputsExists(int id)
        {
            return _context.Outputs.Any(e => e.Code == id);
        }
    }
}
