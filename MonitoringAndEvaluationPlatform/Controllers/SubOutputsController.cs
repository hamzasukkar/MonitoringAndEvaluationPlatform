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
    public class SubOutputsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubOutputsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SubOutputs
        public async Task<IActionResult> Index()
        {
            return View(await _context.SubOutputs.ToListAsync());
        }

        // GET: SubOutputs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subOutputs = await _context.SubOutputs
                .FirstOrDefaultAsync(m => m.Code == id);
            if (subOutputs == null)
            {
                return NotFound();
            }

            return View(subOutputs);
        }

        // GET: SubOutputs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SubOutputs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,SubOutput,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] SubOutput subOutputs)
        {
            if (ModelState.IsValid)
            {
                _context.Add(subOutputs);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(subOutputs);
        }

        // GET: SubOutputs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subOutputs = await _context.SubOutputs.FindAsync(id);
            if (subOutputs == null)
            {
                return NotFound();
            }
            return View(subOutputs);
        }

        // POST: SubOutputs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,SubOutput,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] SubOutput subOutputs)
        {
            if (id != subOutputs.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(subOutputs);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubOutputsExists(subOutputs.Code))
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
            return View(subOutputs);
        }

        // GET: SubOutputs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subOutputs = await _context.SubOutputs
                .FirstOrDefaultAsync(m => m.Code == id);
            if (subOutputs == null)
            {
                return NotFound();
            }

            return View(subOutputs);
        }

        // POST: SubOutputs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subOutputs = await _context.SubOutputs.FindAsync(id);
            if (subOutputs != null)
            {
                _context.SubOutputs.Remove(subOutputs);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SubOutputsExists(int id)
        {
            return _context.SubOutputs.Any(e => e.Code == id);
        }
    }
}
