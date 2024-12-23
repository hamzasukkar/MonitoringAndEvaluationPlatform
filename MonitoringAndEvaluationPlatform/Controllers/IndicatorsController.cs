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
    public class IndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IndicatorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Indicators
        public async Task<IActionResult> Index()
        {
            return View(await _context.Indicators.ToListAsync());
        }

        // GET: Indicators/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indicators = await _context.Indicators
                .FirstOrDefaultAsync(m => m.Code == id);
            if (indicators == null)
            {
                return NotFound();
            }

            return View(indicators);
        }

        // GET: Indicators/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Indicators/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Indicator,Trend,IndicatorsPerformance")] Indicator indicators)
        {
            if (ModelState.IsValid)
            {
                _context.Add(indicators);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(indicators);
        }

        // GET: Indicators/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indicators = await _context.Indicators.FindAsync(id);
            if (indicators == null)
            {
                return NotFound();
            }
            return View(indicators);
        }

        // POST: Indicators/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Indicator,Trend,IndicatorsPerformance")] Indicator indicators)
        {
            if (id != indicators.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(indicators);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IndicatorsExists(indicators.Code))
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
            return View(indicators);
        }

        // GET: Indicators/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indicators = await _context.Indicators
                .FirstOrDefaultAsync(m => m.Code == id);
            if (indicators == null)
            {
                return NotFound();
            }

            return View(indicators);
        }

        // POST: Indicators/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var indicators = await _context.Indicators.FindAsync(id);
            if (indicators != null)
            {
                _context.Indicators.Remove(indicators);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IndicatorsExists(int id)
        {
            return _context.Indicators.Any(e => e.Code == id);
        }
    }
}
