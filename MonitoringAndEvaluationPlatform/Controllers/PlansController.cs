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
    public class PlansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Plans
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Plan.Include(p => p.Activity);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Plans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.Plan
                .Include(p => p.Activity)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // GET: Plans/Create
        public IActionResult Create()
        {
            ViewData["ActivityCode"] = new SelectList(_context.Activity, "Code", "Code");
            return View();
        }

        // POST: Plans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Date,Planned,Realised,ActivityCode")] Plan plan)
        {
            ModelState.Remove(nameof(plan.Activity));

            if (ModelState.IsValid)
            {
                _context.Add(plan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ActivityCode"] = new SelectList(_context.Activity, "Code", "Code", plan.ActivityCode);
            return View(plan);
        }

        // GET: Plans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.Plan.FindAsync(id);
            if (plan == null)
            {
                return NotFound();
            }
            ViewData["ActivityCode"] = new SelectList(_context.Activity, "Code", "Code", plan.ActivityCode);
            return View(plan);
        }

        // POST: Plans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Date,Planned,Realised,ActivityCode")] Plan plan)
        {
            if (id != plan.Code)
            {
                return NotFound();
            }
            ModelState.Remove(nameof(plan.Activity));
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(plan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlanExists(plan.Code))
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
            ViewData["ActivityCode"] = new SelectList(_context.Activity, "Code", "Code", plan.ActivityCode);
            return View(plan);
        }

        // GET: Plans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.Plan
                .Include(p => p.Activity)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // POST: Plans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var plan = await _context.Plan.FindAsync(id);
            if (plan != null)
            {
                _context.Plan.Remove(plan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PlanExists(int id)
        {
            return _context.Plan.Any(e => e.Code == id);
        }
    }
}
