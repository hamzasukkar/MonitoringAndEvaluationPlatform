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
    public class SuperVisorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuperVisorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SuperVisors
        public async Task<IActionResult> Index()
        {
            return View(await _context.SuperVisor.ToListAsync());
        }

        // GET: SuperVisors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var superVisor = await _context.SuperVisor
                .FirstOrDefaultAsync(m => m.Code == id);
            if (superVisor == null)
            {
                return NotFound();
            }

            return View(superVisor);
        }

        // GET: SuperVisors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SuperVisors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name")] SuperVisor superVisor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(superVisor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(superVisor);
        }

        // GET: SuperVisors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var superVisor = await _context.SuperVisor.FindAsync(id);
            if (superVisor == null)
            {
                return NotFound();
            }
            return View(superVisor);
        }

        // POST: SuperVisors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name")] SuperVisor superVisor)
        {
            if (id != superVisor.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(superVisor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SuperVisorExists(superVisor.Code))
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
            return View(superVisor);
        }

        // GET: SuperVisors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var superVisor = await _context.SuperVisor
                .FirstOrDefaultAsync(m => m.Code == id);
            if (superVisor == null)
            {
                return NotFound();
            }

            return View(superVisor);
        }

        // POST: SuperVisors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var superVisor = await _context.SuperVisor.FindAsync(id);
            if (superVisor != null)
            {
                _context.SuperVisor.Remove(superVisor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SuperVisorExists(int id)
        {
            return _context.SuperVisor.Any(e => e.Code == id);
        }
    }
}
