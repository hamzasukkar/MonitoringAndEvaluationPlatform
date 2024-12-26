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
    public class FrameworksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FrameworksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Frameworks
        public async Task<IActionResult> Index()
        {
            return View(await _context.Freamework.ToListAsync());
        }
        // GET: Monitoring
        public async Task<IActionResult> Monitoring()
        {
            return View(await _context.Freamework.ToListAsync());
        }

        public async Task<IActionResult> Dashboard()
        {
            return View(await _context.Freamework.ToListAsync());
        }

        // GET: Frameworks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var freamework = await _context.Freamework
                .FirstOrDefaultAsync(m => m.Code == id);
            if (freamework == null)
            {
                return NotFound();
            }

            return View(freamework);
        }

        // GET: Frameworks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Frameworks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Framework,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Framework freamework)
        {
            if (ModelState.IsValid)
            {
                _context.Add(freamework);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(freamework);
        }

        // GET: Frameworks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var freamework = await _context.Freamework.FindAsync(id);
            if (freamework == null)
            {
                return NotFound();
            }
            return View(freamework);
        }

        // POST: Frameworks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Framework,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Framework freamework)
        {
            if (id != freamework.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(freamework);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FreameworkExists(freamework.Code))
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
            return View(freamework);
        }

        // GET: Frameworks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var freamework = await _context.Freamework
                .FirstOrDefaultAsync(m => m.Code == id);
            if (freamework == null)
            {
                return NotFound();
            }

            return View(freamework);
        }

        // POST: Frameworks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var freamework = await _context.Freamework.FindAsync(id);
            if (freamework != null)
            {
                _context.Freamework.Remove(freamework);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FreameworkExists(int id)
        {
            return _context.Freamework.Any(e => e.Code == id);
        }
    }
}
