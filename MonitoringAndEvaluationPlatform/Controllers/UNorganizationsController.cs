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
    public class UNorganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UNorganizationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: UNorganizations
        public async Task<IActionResult> Index()
        {
            return View(await _context.UNorganizations.ToListAsync());
        }

        // GET: UNorganizations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uNorganization = await _context.UNorganizations
                .FirstOrDefaultAsync(m => m.Code == id);
            if (uNorganization == null)
            {
                return NotFound();
            }

            return View(uNorganization);
        }

        // GET: UNorganizations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UNorganizations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] UNorganization uNorganization)
        {
            if (ModelState.IsValid)
            {
                _context.Add(uNorganization);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(uNorganization);
        }

        // GET: UNorganizations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uNorganization = await _context.UNorganizations.FindAsync(id);
            if (uNorganization == null)
            {
                return NotFound();
            }
            return View(uNorganization);
        }

        // POST: UNorganizations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] UNorganization uNorganization)
        {
            if (id != uNorganization.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(uNorganization);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UNorganizationExists(uNorganization.Code))
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
            return View(uNorganization);
        }

        // GET: UNorganizations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uNorganization = await _context.UNorganizations
                .FirstOrDefaultAsync(m => m.Code == id);
            if (uNorganization == null)
            {
                return NotFound();
            }

            return View(uNorganization);
        }

        // POST: UNorganizations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uNorganization = await _context.UNorganizations.FindAsync(id);
            if (uNorganization != null)
            {
                _context.UNorganizations.Remove(uNorganization);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UNorganizationExists(int id)
        {
            return _context.UNorganizations.Any(e => e.Code == id);
        }
    }
}
