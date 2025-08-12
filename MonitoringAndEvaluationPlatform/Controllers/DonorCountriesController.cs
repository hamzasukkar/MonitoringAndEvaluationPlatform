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
    public class DonorCountriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonorCountriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DonorCountries
        public async Task<IActionResult> Index()
        {
            return View(await _context.DonorCountries.ToListAsync());
        }

        // GET: DonorCountries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donorCountry = await _context.DonorCountries
                .FirstOrDefaultAsync(m => m.Code == id);
            if (donorCountry == null)
            {
                return NotFound();
            }

            return View(donorCountry);
        }

        // GET: DonorCountries/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DonorCountries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] DonorCountry donorCountry)
        {
            if (ModelState.IsValid)
            {
                _context.Add(donorCountry);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(donorCountry);
        }

        // GET: DonorCountries/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donorCountry = await _context.DonorCountries.FindAsync(id);
            if (donorCountry == null)
            {
                return NotFound();
            }
            return View(donorCountry);
        }

        // POST: DonorCountries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] DonorCountry donorCountry)
        {
            if (id != donorCountry.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(donorCountry);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonorCountryExists(donorCountry.Code))
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
            return View(donorCountry);
        }

        // GET: DonorCountries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donorCountry = await _context.DonorCountries
                .FirstOrDefaultAsync(m => m.Code == id);
            if (donorCountry == null)
            {
                return NotFound();
            }

            return View(donorCountry);
        }

        // POST: DonorCountries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donorCountry = await _context.DonorCountries.FindAsync(id);
            if (donorCountry != null)
            {
                _context.DonorCountries.Remove(donorCountry);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DonorCountryExists(int id)
        {
            return _context.DonorCountries.Any(e => e.Code == id);
        }
    }
}
