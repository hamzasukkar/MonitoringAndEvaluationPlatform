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
    public class MinistriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MinistriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Ministries
        public async Task<IActionResult> Index()
        {
            return View(await _context.Ministrie.ToListAsync());
        }

        // GET: Ministries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ministrie = await _context.Ministrie
                .FirstOrDefaultAsync(m => m.Code == id);
            if (ministrie == null)
            {
                return NotFound();
            }

            return View(ministrie);
        }

        // GET: Ministries/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ministries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Partner,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Ministrie ministrie)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ministrie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ministrie);
        }

        // GET: Ministries/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ministrie = await _context.Ministrie.FindAsync(id);
            if (ministrie == null)
            {
                return NotFound();
            }
            return View(ministrie);
        }

        // POST: Ministries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Partner,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Ministrie ministrie)
        {
            if (id != ministrie.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ministrie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MinistrieExists(ministrie.Code))
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
            return View(ministrie);
        }

        // GET: Ministries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ministrie = await _context.Ministrie
                .FirstOrDefaultAsync(m => m.Code == id);
            if (ministrie == null)
            {
                return NotFound();
            }

            return View(ministrie);
        }

        // POST: Ministries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ministrie = await _context.Ministrie.FindAsync(id);
            if (ministrie != null)
            {
                _context.Ministrie.Remove(ministrie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MinistrieExists(int id)
        {
            return _context.Ministrie.Any(e => e.Code == id);
        }
    }
}
