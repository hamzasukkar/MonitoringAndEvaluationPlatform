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
    public class SectorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SectorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sectors
        public async Task<IActionResult> Index()
        {
            return View(await _context.Sectors.ToListAsync());
        }

        public async Task<IActionResult> ResultIndex(int code)
        {
            var sector = await _context.Sectors.FirstOrDefaultAsync(s => s.Code == code);

            if (sector == null)
            {
                return NotFound();
            }

            return View(new List<Sector> { sector });
        }

        // GET: Sectors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sector = await _context.Sectors
                .FirstOrDefaultAsync(m => m.Code == id);
            if (sector == null)
            {
                return NotFound();
            }

            return View(sector);
        }

        // GET: Sectors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sectors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Sector sector)
        {
            if (ModelState.IsValid)
            {
                _context.Add(sector);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(sector);
        }

        // GET: Sectors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sector = await _context.Sectors.FindAsync(id);
            if (sector == null)
            {
                return NotFound();
            }
            return View(sector);
        }

        // POST: Sectors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Sector sector)
        {
            if (id != sector.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sector);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SectorExists(sector.Code))
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
            return View(sector);
        }

        // GET: Sectors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sector = await _context.Sectors
                .FirstOrDefaultAsync(m => m.Code == id);
            if (sector == null)
            {
                return NotFound();
            }

            return View(sector);
        }

        // POST: Sectors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sector = await _context.Sectors.FindAsync(id);
            if (sector != null)
            {
                _context.Sectors.Remove(sector);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SectorExists(int id)
        {
            return _context.Sectors.Any(e => e.Code == id);
        }

        // Inline Operations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(string EN_Name, string AR_Name)
        {
            if (string.IsNullOrWhiteSpace(EN_Name) || string.IsNullOrWhiteSpace(AR_Name))
            {
                return Json(new { success = false, message = "English and Arabic names are required." });
            }

            var sector = new Sector
            {
                EN_Name = EN_Name,
                AR_Name = AR_Name
            };

            try
            {
                _context.Sectors.Add(sector);
                await _context.SaveChangesAsync();
                return Json(new { success = true, sector = new { sector.Code, sector.EN_Name, sector.AR_Name } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating sector: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InlineEdit(int id, string field, string value)
        {
            var sector = await _context.Sectors.FindAsync(id);
            if (sector == null)
                return Json(new { success = false, message = "Sector not found" });

            switch (field.ToLower())
            {
                case "en_name":
                    sector.EN_Name = value;
                    break;
                case "ar_name":
                    sector.AR_Name = value;
                    break;
                default:
                    return Json(new { success = false, message = "Invalid field" });
            }

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

        [HttpPost]
        public async Task<IActionResult> InlineDelete(int id)
        {
            var sector = await _context.Sectors.FindAsync(id);
            if (sector == null)
                return Json(new { success = false, message = "Sector not found" });

            try
            {
                _context.Sectors.Remove(sector);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickUpdate(int id, string enName, string arName)
        {
            var sector = await _context.Sectors.FindAsync(id);
            if (sector == null)
                return Json(new { success = false, message = "Sector not found" });

            if (string.IsNullOrWhiteSpace(enName) || string.IsNullOrWhiteSpace(arName))
                return Json(new { success = false, message = "Both names are required" });

            sector.EN_Name = enName;
            sector.AR_Name = arName;

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
    }
}
