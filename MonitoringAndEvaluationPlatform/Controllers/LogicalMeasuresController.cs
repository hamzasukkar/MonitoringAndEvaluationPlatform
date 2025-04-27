using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class LogicalMeasuresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogicalMeasuresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LogicalMeasures
        public async Task<IActionResult> Index()
        {
            return View(await _context.logicalMeasures.ToListAsync());
        }

        // GET: LogicalMeasures/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalMeasure = await _context.logicalMeasures
                .FirstOrDefaultAsync(m => m.Code == id);
            if (logicalMeasure == null)
            {
                return NotFound();
            }

            return View(logicalMeasure);
        }

        // GET: LogicalMeasures/Create
        public IActionResult Create(int? indicatorCode)
        {
            if (indicatorCode.HasValue)
            {
                ViewBag.IndicatorCode = indicatorCode.Value; // ✅ send the value directly
            }
            else
            {
                ViewData["LogicaIndicator"] = new SelectList(_context.logicalFrameworkIndicators, "IndicatorCode", "Name");
            }
            return View();
        }

        // POST: LogicalMeasures/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LogicalMeasure logicalMeasure)
        {
            ModelState.Remove(nameof(logicalMeasure.LogicalFrameworkIndicator));

            if (ModelState.IsValid)
            {
                _context.Add(logicalMeasure);
                await _context.SaveChangesAsync();

                //After saving the new LogicalMeasure
                await UpdateLogicalFrameworkIndicatorPerformance(logicalMeasure.LogicalFrameworkIndicatorIndicatorCode);
                await UpdateProjectPerformanceAsync(logicalMeasure.LogicalFrameworkIndicator.LogicalFramework.ProjectID);

                return RedirectToAction(nameof(LogicalFrameworkIndicatorsController.Details), "LogicalFrameworkIndicators", new { id = logicalMeasure.LogicalFrameworkIndicatorIndicatorCode });
            }

            return View(logicalMeasure);

        }

        private async Task UpdateLogicalFrameworkIndicatorPerformance(int logicalIndicatorId)
        {
            var logicalIndicator = await _context.logicalFrameworkIndicators
                .Include(lfi => lfi.logicalMeasures)
                .FirstOrDefaultAsync(lfi => lfi.IndicatorCode == logicalIndicatorId);

            if (logicalIndicator != null)
            {
                double totalReal = logicalIndicator.logicalMeasures
                    .Where(m => m.ValueType == MeasureValueType.Real)
                    .Sum(m => m.Value);

                if (logicalIndicator.Target > 0)
                {
                    logicalIndicator.Performance = (totalReal / logicalIndicator.Target) * 100;
                }
                else
                {
                    logicalIndicator.Performance = 0;
                }

                _context.Update(logicalIndicator);
                await _context.SaveChangesAsync();
                await UpdateLogicalFrameworkPerformanceAsync(logicalIndicator.LogicalFrameworkCode);
            }
        }

        public async Task UpdateLogicalFrameworkPerformanceAsync(int frameworkId)
        {
            // 1. Compute average Performance of active indicators (returns null if none)
            double averagePerformance = await _context.logicalFrameworkIndicators
                .Where(i => i.LogicalFrameworkCode == frameworkId && i.Active)
                .Select(i => (double?)i.Performance)
                .AverageAsync() ?? 0.0;

            // 2. Load the parent LogicalFramework and update its Performance
            var framework = await _context.logicalFrameworks.FindAsync(frameworkId);
            if (framework != null)
            {
                framework.Performance = averagePerformance;
                // 3. Save the updated Performance
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateProjectPerformanceAsync(int projectId)
        {
            // 1. Get all LogicalFrameworks for this project
            var logicalFrameworks = await _context.logicalFrameworks
                .Where(lf => lf.ProjectID == projectId)
                .ToListAsync();

            if (logicalFrameworks == null || logicalFrameworks.Count == 0)
                return; // No frameworks, no update needed

            // 2. Calculate average Performance (safely)
            double averagePerformance = logicalFrameworks
                .Where(lf => lf.Performance > 0) // Optional: ignore frameworks with 0 if you want
                .Select(lf => (double?)lf.Performance)
                .Average() ?? 0.0;

            // 3. Find the project and update
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                project.performance = averagePerformance;
                await _context.SaveChangesAsync();
            }
        }


        // GET: LogicalMeasures/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalMeasure = await _context.logicalMeasures.FindAsync(id);
            if (logicalMeasure == null)
            {
                return NotFound();
            }
            return View(logicalMeasure);
        }

        // POST: LogicalMeasures/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,LogicalMeasure logicalMeasure)
        {
            if (id != logicalMeasure.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(logicalMeasure);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LogicalMeasureExists(logicalMeasure.Code))
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
            return View(logicalMeasure);
        }

        // GET: LogicalMeasures/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalMeasure = await _context.logicalMeasures
                .FirstOrDefaultAsync(m => m.Code == id);
            if (logicalMeasure == null)
            {
                return NotFound();
            }

            return View(logicalMeasure);
        }

        // POST: LogicalMeasures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var logicalMeasure = await _context.logicalMeasures.FindAsync(id);
            if (logicalMeasure != null)
            {
                _context.logicalMeasures.Remove(logicalMeasure);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LogicalMeasureExists(int id)
        {
            return _context.logicalMeasures.Any(e => e.Code == id);
        }
    }
}
