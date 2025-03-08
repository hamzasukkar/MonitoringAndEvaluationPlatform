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
    public class MinistrysController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MinistrysController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Ministrys
        public async Task<IActionResult> Index()
        {
            return View(await _context.Ministry.ToListAsync());
        }

        // GET: Ministrys/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var Ministry = await _context.Ministry
                .FirstOrDefaultAsync(m => m.Code == id);
            if (Ministry == null)
            {
                return NotFound();
            }

            return View(Ministry);
        }

        // GET: Ministrys/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ministrys/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Partner,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Ministry Ministry)
        {
            if (ModelState.IsValid)
            {
                _context.Add(Ministry);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(Ministry);
        }

        // GET: Ministrys/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var Ministry = await _context.Ministry.FindAsync(id);
            if (Ministry == null)
            {
                return NotFound();
            }
            return View(Ministry);
        }

        // POST: Ministrys/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Partner,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Ministry Ministry)
        {
            if (id != Ministry.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(Ministry);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MinistryExists(Ministry.Code))
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
            return View(Ministry);
        }

        // GET: Ministrys/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var Ministry = await _context.Ministry
                .FirstOrDefaultAsync(m => m.Code == id);
            if (Ministry == null)
            {
                return NotFound();
            }

            return View(Ministry);
        }

        // POST: Ministrys/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var Ministry = await _context.Ministry.FindAsync(id);
            if (Ministry != null)
            {
                _context.Ministry.Remove(Ministry);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MinistryExists(int id)
        {
            return _context.Ministry.Any(e => e.Code == id);
        }
    }
}
