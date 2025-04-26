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
    public class LogicalFrameworkIndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogicalFrameworkIndicatorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LogicalFrameworkIndicators
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.logicalFrameworkIndicators.Include(l => l.LogicalFramework);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: LogicalFrameworkIndicators/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFrameworkIndicator = await _context.logicalFrameworkIndicators
                .Include(l => l.LogicalFramework)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (logicalFrameworkIndicator == null)
            {
                return NotFound();
            }

            return View(logicalFrameworkIndicator);
        }

        // GET: LogicalFrameworkIndicators/Create
        public IActionResult Create(int logicalFrameworkCode)
        {
            var logicalFramework = _context.LogicalFramework.FirstOrDefault(lf => lf.Code == logicalFrameworkCode);
            if (logicalFramework == null)
            {
                return NotFound();
            }

            ViewBag.LogicalFrameworkName = logicalFramework.Name;
            ViewBag.LogicalFrameworkCode = logicalFrameworkCode;

            var relatedIndicators = _context.logicalFrameworkIndicators
                .Where(ind => ind.LogicalFrameworkCode == logicalFrameworkCode)
                .ToList();

            ViewBag.RelatedIndicators = relatedIndicators;

            var model = new LogicalFrameworkIndicator
            {
                LogicalFrameworkCode = logicalFrameworkCode
            };

            return View(model);
        }


        // POST: LogicalFrameworkIndicators/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(LogicalFrameworkIndicator indicator)
        {
            if (ModelState.IsValid || true)
            {
                _context.Add(indicator);
                _context.SaveChanges();

                TempData["Success"] = "Indicator created successfully!";

                // Stay on the same page and reload related indicators
                return RedirectToAction("Create", new { logicalFrameworkCode = indicator.LogicalFrameworkCode });
            }

            // If model state failed, re-fetch data to keep page functional
            var logicalFramework = _context.LogicalFramework.FirstOrDefault(lf => lf.Code == indicator.LogicalFrameworkCode);
            ViewBag.LogicalFrameworkName = logicalFramework?.Name ?? "";
            ViewBag.LogicalFrameworkCode = indicator.LogicalFrameworkCode;
            ViewBag.RelatedIndicators = _context.logicalFrameworkIndicators
                .Where(ind => ind.Code == indicator.LogicalFrameworkCode)
                .ToList();

            return View(indicator);
        }


        // GET: LogicalFrameworkIndicators/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFrameworkIndicator = await _context.logicalFrameworkIndicators.FindAsync(id);
            if (logicalFrameworkIndicator == null)
            {
                return NotFound();
            }
            ViewData["LogicalFrameworkCode"] = new SelectList(_context.logicalFrameworks, "Code", "Code", logicalFrameworkIndicator.LogicalFrameworkCode);
            return View(logicalFrameworkIndicator);
        }

        // POST: LogicalFrameworkIndicators/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Source,Performance,Weight,LogicalFrameworkCode,IsCommon,Active,Target,TargetYear,GAGRA,GAGRR,Concept,Description,MethodOfComputation,Comment")] LogicalFrameworkIndicator logicalFrameworkIndicator)
        {
            if (id != logicalFrameworkIndicator.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(logicalFrameworkIndicator);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LogicalFrameworkIndicatorExists(logicalFrameworkIndicator.Code))
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
            ViewData["LogicalFrameworkCode"] = new SelectList(_context.logicalFrameworks, "Code", "Code", logicalFrameworkIndicator.LogicalFrameworkCode);
            return View(logicalFrameworkIndicator);
        }

        // GET: LogicalFrameworkIndicators/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFrameworkIndicator = await _context.logicalFrameworkIndicators
                .Include(l => l.LogicalFramework)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (logicalFrameworkIndicator == null)
            {
                return NotFound();
            }

            return View(logicalFrameworkIndicator);
        }

        // POST: LogicalFrameworkIndicators/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var logicalFrameworkIndicator = await _context.logicalFrameworkIndicators.FindAsync(id);
            if (logicalFrameworkIndicator != null)
            {
                _context.logicalFrameworkIndicators.Remove(logicalFrameworkIndicator);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LogicalFrameworkIndicatorExists(int id)
        {
            return _context.logicalFrameworkIndicators.Any(e => e.Code == id);
        }
    }
}
