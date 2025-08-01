﻿using System;
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
            return View(await _context.logicalFrameworks.ToListAsync());
        }

        // GET: LogicalFrameworks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFramework = await _context.logicalFrameworks
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
            var project = _context.Projects.FirstOrDefault(p => p.ProjectID == projectId);
            if (project == null)
                return NotFound();

            ViewBag.ProjectID = projectId;

            var relatedFrameworks = _context.logicalFrameworks
                .Where(lf => lf.ProjectID == projectId)
                .ToList();

            ViewBag.RelatedFrameworks = relatedFrameworks;

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
                _context.logicalFrameworks.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Logical Framework created successfully!";
                return RedirectToAction("Create", "LogicalFrameworks", new { projectId = model.ProjectID });
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

            var logicalFramework = await _context.logicalFrameworks.FindAsync(id);
            if (logicalFramework == null)
            {
                return NotFound();
            }
            return View(logicalFramework);
        }
        [HttpPost]
        public async Task<IActionResult> EditNameInline(int code, string name)
        {
            var item = await _context.logicalFrameworks.FindAsync(code);
            if (item == null)
                return Json(new { success = false, message = "Logical Framework not found." });

            item.Name = name;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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

            var logicalFramework = await _context.logicalFrameworks
                .FirstOrDefaultAsync(m => m.Code == id);
            if (logicalFramework == null)
            {
                return NotFound();
            }

            return View(logicalFramework);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var framework = await _context.logicalFrameworks.FindAsync(id);
            if (framework == null)
            {
                return NotFound();
            }

            _context.logicalFrameworks.Remove(framework);
            await _context.SaveChangesAsync();

            // ✅ Redirect to the correct page (adjust "Index" if your view is different)
            return RedirectToAction("Create");
        }


        private bool LogicalFrameworkExists(int id)
        {
            return _context.logicalFrameworks.Any(e => e.Code == id);
        }
    }
}
