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
    public class MeasuresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MeasuresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Measures
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Measure.Include(m => m.Indicator);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Measures/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measure = await _context.Measure
                .Include(m => m.Indicator)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (measure == null)
            {
                return NotFound();
            }

            return View(measure);
        }

        // GET: Measures/Create
        public IActionResult Create()
        {
            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "Code", "Name");
            return View();
        }

        // POST: Measures/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Date,Value,ValueType,IndicatorCode")] Measure measure)
        {
            ModelState.Remove(nameof(measure.Indicator));
            if (ModelState.IsValid)
            {
                _context.Add(measure);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "Code", "Code", measure.IndicatorCode);
            return View(measure);
        }

        // GET: Measures/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measure = await _context.Measure.FindAsync(id);
            if (measure == null)
            {
                return NotFound();
            }
            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "Code", "Code", measure.IndicatorCode);
            return View(measure);
        }

        // POST: Measures/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Date,Value,ValueType,IndicatorCode")] Measure measure)
        {
            if (id != measure.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(measure);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeasureExists(measure.Code))
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
            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "Code", "Code", measure.IndicatorCode);
            return View(measure);
        }

        // GET: Measures/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measure = await _context.Measure
                .Include(m => m.Indicator)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (measure == null)
            {
                return NotFound();
            }

            return View(measure);
        }

        // POST: Measures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var measure = await _context.Measure.FindAsync(id);
            if (measure != null)
            {
                _context.Measure.Remove(measure);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MeasureExists(int id)
        {
            return _context.Measure.Any(e => e.Code == id);
        }
    }
}
