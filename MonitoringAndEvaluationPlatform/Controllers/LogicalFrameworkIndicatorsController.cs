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
            var applicationDbContext = _context.LogicalFrameworkIndicator.Include(l => l.LogicalFramework);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: LogicalFrameworkIndicators/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFrameworkIndicator = await _context.LogicalFrameworkIndicator
                .Include(l => l.LogicalFramework)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (logicalFrameworkIndicator == null)
            {
                return NotFound();
            }

            return View(logicalFrameworkIndicator);
        }

        // GET: LogicalFrameworkIndicators/Create
        public IActionResult Create()
        {
            ViewData["LogicalFrameworkCode"] = new SelectList(_context.LogicalFramework, "Code", "Name");
            return View();
        }

        // POST: LogicalFrameworkIndicators/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LogicalFrameworkIndicator logicalFrameworkIndicator)
        {
            if (ModelState.IsValid || true)
            {
                _context.Add(logicalFrameworkIndicator);

                await _context.SaveChangesAsync();

                var logicalFramework = _context.LogicalFramework.FirstOrDefault(l => l.Code == logicalFrameworkIndicator.LogicalFrameworkCode);

                if(logicalFramework!=null)
                {
                    var logicalFrameworkIndicators = _context.LogicalFrameworkIndicator.Where(fi=>fi.LogicalFrameworkCode== logicalFrameworkIndicator.LogicalFrameworkCode).ToList();

                    int logicalFrameworkPerformance = 0;

                    foreach (var item in logicalFrameworkIndicators)
                    {
                        logicalFrameworkPerformance += item.IndicatorsPerformance;
                    }

                    logicalFramework.IndicatorsPerformance = logicalFrameworkPerformance/ logicalFrameworkIndicators.Count;
                    _context.Update(logicalFramework);
                    await _context.SaveChangesAsync();

                }


                var program = _context.Project.FirstOrDefault(p => p.ProjectID == logicalFramework.ProjectID);

                if (program!=null)
                {
                    int programPerformance = 0;

                    var LogicalFrameworks = _context.LogicalFramework.Where(f=>f.ProjectID== logicalFramework.ProjectID).ToList();

                    foreach (var item in LogicalFrameworks)
                    {
                        programPerformance += item.IndicatorsPerformance;
                    }

                    program.performance = programPerformance /  LogicalFrameworks.Count;

                    _context.Update(program);

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["LogicalFrameworkCode"] = new SelectList(_context.LogicalFramework, "Code", "Code", logicalFrameworkIndicator.LogicalFrameworkCode);
            return View(logicalFrameworkIndicator);
        }

        // GET: LogicalFrameworkIndicators/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFrameworkIndicator = await _context.LogicalFrameworkIndicator.FindAsync(id);
            if (logicalFrameworkIndicator == null)
            {
                return NotFound();
            }
            ViewData["LogicalFrameworkCode"] = new SelectList(_context.LogicalFramework, "Code", "Code", logicalFrameworkIndicator.LogicalFrameworkCode);
            return View(logicalFrameworkIndicator);
        }

        // POST: LogicalFrameworkIndicators/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,LogicalFrameworkIndicator logicalFrameworkIndicator)
        {
            if (id != logicalFrameworkIndicator.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid || true)
            {
                try
                {
                    _context.Update(logicalFrameworkIndicator);

                    var logicalFramework = _context.LogicalFramework.FirstOrDefault(l=>l.Code== logicalFrameworkIndicator.LogicalFrameworkCode);
                    var program = _context.Project.FirstOrDefault(p => p.ProjectID == logicalFramework.ProjectID);

                    if (program != null)
                    {
                        int programPerformance = 0;

                        var LogicalFrameworks = _context.LogicalFramework.Where(f => f.ProjectID == logicalFrameworkIndicator.LogicalFramework.ProjectID).ToList();

                        foreach (var item in LogicalFrameworks)
                        {
                            programPerformance += item.IndicatorsPerformance;
                        }

                        program.performance = programPerformance / LogicalFrameworks.Count;
                        _context.Update(program);
                        await _context.SaveChangesAsync();
                    }

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
            ViewData["LogicalFrameworkCode"] = new SelectList(_context.LogicalFramework, "Code", "Code", logicalFrameworkIndicator.LogicalFrameworkCode);
            return View(logicalFrameworkIndicator);
        }

        // GET: LogicalFrameworkIndicators/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFrameworkIndicator = await _context.LogicalFrameworkIndicator
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
            var logicalFrameworkIndicator = await _context.LogicalFrameworkIndicator.FindAsync(id);
            if (logicalFrameworkIndicator != null)
            {
                _context.LogicalFrameworkIndicator.Remove(logicalFrameworkIndicator);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LogicalFrameworkIndicatorExists(int id)
        {
            return _context.LogicalFrameworkIndicator.Any(e => e.Code == id);
        }
    }
}
