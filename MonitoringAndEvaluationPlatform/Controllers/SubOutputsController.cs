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
    public class SubOutputsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubOutputsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SubOutputs
        public async Task<IActionResult> Index(int? id)
        {
            if (id == null)
            {
                var applicationDbContext = _context.SubOutputs.Include(s => s.Output);
                return View(await applicationDbContext.ToListAsync());
            }
            var subOutputs = await _context.SubOutputs
                 .Include(s => s.Output)
                 .Include(s => s.Indicators)
                 .Where(m => m.Output.Outcome.FrameworkCode == id).ToListAsync();
            if (subOutputs == null)
            {
                return NotFound();
            }

            return View(subOutputs);
        }

        // GET: SubOutputs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subOutput = await _context.SubOutputs
                .Include(s => s.Output)
                .Include(s => s.Indicators)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (subOutput == null)
            {
                return NotFound();
            }

            return View(subOutput);
        }

        // GET: SubOutputs/Create
        public IActionResult Create()
        {
            ViewData["OutputCode"] = new SelectList(_context.Outputs, "Code", "Name");
            return View();
        }

        // POST: SubOutputs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Trend,OutputCode,Weight")] SubOutput subOutput)
        {
            ModelState.Remove(nameof(subOutput.Output));

            if (ModelState.IsValid)
            {
                _context.Add(subOutput);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OutputCode"] = new SelectList(_context.Outputs, "Code", "Name", subOutput.OutputCode);
            return View(subOutput);
        }

        // GET: SubOutputs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subOutput = await _context.SubOutputs.FindAsync(id);
            if (subOutput == null)
            {
                return NotFound();
            }
            ViewData["OutputCode"] = new SelectList(_context.Outputs, "Code", "Name", subOutput.OutputCode);
            return View(subOutput);
        }

        // POST: SubOutputs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment,OutputCode")] SubOutput subOutput)
        {
            if (id != subOutput.Code)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(subOutput.Output));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(subOutput);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubOutputExists(subOutput.Code))
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
            ViewData["OutputCode"] = new SelectList(_context.Outputs, "Code", "Name", subOutput.OutputCode);
            return View(subOutput);
        }

        // GET: SubOutputs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subOutput = await _context.SubOutputs
                .Include(s => s.Output)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (subOutput == null)
            {
                return NotFound();
            }

            return View(subOutput);
        }

        // POST: SubOutputs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subOutput = await _context.SubOutputs.FindAsync(id);
            if (subOutput != null)
            {
                _context.SubOutputs.Remove(subOutput);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SubOutputExists(int id)
        {
            return _context.SubOutputs.Any(e => e.Code == id);
        }
    }
}
