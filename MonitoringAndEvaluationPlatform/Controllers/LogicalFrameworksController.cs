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
    public class LogicalFrameworksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogicalFrameworksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LogicalFrameworks
        public async Task<IActionResult> Index()
        {
            return View(await _context.LogicalFramework.ToListAsync());
        }

        // GET: LogicalFrameworks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFramework = await _context.LogicalFramework
                .FirstOrDefaultAsync(m => m.Code == id);
            if (logicalFramework == null)
            {
                return NotFound();
            }

            return View(logicalFramework);
        }

        // GET: LogicalFrameworks/Create
        public IActionResult Create(int projectId)
        {
            var model = new LogicalFramework
            {
                ProjectID = projectId
            };

            return View(model);
        }

        // POST: LogicalFrameworks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LogicalFramework model)
        {
            if (ModelState.IsValid)
            {
                _context.LogicalFramework.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Projects", new { id = model.ProjectID });
            }

            return View(model);
        }

        // GET: LogicalFrameworks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFramework = await _context.LogicalFramework.FindAsync(id);
            if (logicalFramework == null)
            {
                return NotFound();
            }
            return View(logicalFramework);
        }

        // POST: LogicalFrameworks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Type,Performance,ProjectID")] LogicalFramework logicalFramework)
        {
            if (id != logicalFramework.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(logicalFramework);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LogicalFrameworkExists(logicalFramework.Code))
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
            return View(logicalFramework);
        }

        // GET: LogicalFrameworks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFramework = await _context.LogicalFramework
                .FirstOrDefaultAsync(m => m.Code == id);
            if (logicalFramework == null)
            {
                return NotFound();
            }

            return View(logicalFramework);
        }

        // POST: LogicalFrameworks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var logicalFramework = await _context.LogicalFramework.FindAsync(id);
            if (logicalFramework != null)
            {
                _context.LogicalFramework.Remove(logicalFramework);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LogicalFrameworkExists(int id)
        {
            return _context.LogicalFramework.Any(e => e.Code == id);
        }
    }
}
