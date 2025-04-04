﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OutputsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OutputsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Outputs
        public async Task<IActionResult> Index(int? id)
        {
            if (id == null)
            {
                var applicationDbContext = _context.Outputs.Include(o => o.Outcome);
                return View(await applicationDbContext.ToListAsync());
            }

            ViewBag.SelectedOutcomeCode = id; // Store it for use in the view

            var outputs = await _context.Outputs
                .Include(o => o.Outcome)
                .Include(o => o.SubOutputs)
                .Where(m => m.Outcome.FrameworkCode == id).ToListAsync();

            if (outputs == null)
            {
                return NotFound();
            }

            return View(outputs);

        }

        // GET: Outputs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var output = await _context.Outputs
                .Include(o => o.Outcome)
                .Include(o => o.SubOutputs)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (output == null)
            {
                return NotFound();
            }

            return View(output);
        }

        // GET: Outputs/Create
        public IActionResult Create(int? id)
        {
            ViewData["OutcomeCode"] = new SelectList(_context.Outcomes, "Code", "Name");

            var outcomes = _context.Outcomes.ToList();

            // Populate dropdown only if no framework is preselected
            ViewData["OutcomeCode"] = id == null
                ? new SelectList(outcomes, "Code", "Name")
                : new SelectList(outcomes, "Code", "Name", id);

            // Pass the selected framework code to the view
            ViewBag.SelectedOutcomeCode = id;

            return View();
        }

        // POST: Outputs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Trend,OutcomeCode,Weight")] Output output)
        {
            ModelState.Remove(nameof(output.Outcome));

            if (ModelState.IsValid)
            {
                _context.Add(output);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { id = output.OutcomeCode });
            }
            ViewData["OutcomeCode"] = new SelectList(_context.Outcomes, "Code", "Name", output.OutcomeCode);
            return View(output);
        }

        // GET: Outputs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var output = await _context.Outputs.FindAsync(id);
            if (output == null)
            {
                return NotFound();
            }
            ViewData["OutcomeCode"] = new SelectList(_context.Outcomes, "Code", "Name", output.OutcomeCode);
            return View(output);
        }

        // POST: Outputs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment,OutcomeCode")] Output output)
        {
            if (id != output.Code)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(output.Outcome));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(output);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OutputExists(output.Code))
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
            ViewData["OutcomeCode"] = new SelectList(_context.Outcomes, "Code", "Name", output.OutcomeCode);
            return View(output);
        }

        // GET: Outputs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var output = await _context.Outputs
                .Include(o => o.Outcome)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (output == null)
            {
                return NotFound();
            }

            return View(output);
        }

        // POST: Outputs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var output = await _context.Outputs.FindAsync(id);
            if (output != null)
            {
                _context.Outputs.Remove(output);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OutputExists(int id)
        {
            return _context.Outputs.Any(e => e.Code == id);
        }
    }
}
