using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    // Controller
    public class MeasuresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MonitoringService _monitoringService;

        public MeasuresController(ApplicationDbContext context, MonitoringService monitoringService)
        {
            _context = context;
            _monitoringService = monitoringService;
        }

        [HttpPost("add-measure")]
        public async Task<IActionResult> AddMeasure([FromBody] AddMeasureDto dto)
        {
            await _monitoringService.AddMeasureToIndicator(dto.IndicatorId, dto.Value, MeasureValueType.Real);
            return Ok("Measure added and Indicator Performance updated");
        }

        // DTO
        public class AddMeasureDto
        {
            public int IndicatorId { get; set; }
            public double Value { get; set; }
        }

        // GET: Measures
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Measures.Include(m => m.Indicator);
            return View(await applicationDbContext.ToListAsync());
        }


        // GET: Measures/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measure = await _context.Measures
                .Include(m => m.Indicator)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (measure == null)
            {
                return NotFound();
            }

            return View(measure);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromDetails(Measure measure)
        {
            ModelState.Remove(nameof(measure.Indicator));
            if (ModelState.IsValid)
            {
                _context.Add(measure);
                await _context.SaveChangesAsync();
                return Ok(); // for AJAX
            }

            return BadRequest("Invalid input");
        }

        // GET: Measures/Create
        public IActionResult Create()
        {
            ViewData["Indicators"] = new SelectList(_context.Indicators, "IndicatorCode", "Name");
            return View();
        }

        // POST: Measures/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Date,Value,IndicatorCode")] Measure measure)
        {
            ModelState.Remove(nameof(measure.Indicator));

            // Set the measure as Real (only type available now)
            measure.ValueType = MeasureValueType.Real;

            if (ModelState.IsValid)
            {
                _context.Measures.Add(measure);
                await _context.SaveChangesAsync();

                // Update indicator performance after adding the measure
                await _monitoringService.UpdateIndicatorPerformance(measure.IndicatorCode);
                
                TempData["SuccessMessage"] = "Measure added successfully and indicator performance has been updated.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "IndicatorCode", "Name", measure.IndicatorCode);
            return View(measure);
        }


        // GET: Measures/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measure = await _context.Measures.FindAsync(id);
            if (measure == null)
            {
                return NotFound();
            }
            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "IndicatorCode", "Name", measure.IndicatorCode);
            return View(measure);
        }

        // POST: Measures/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Measure measure)
        {
            if (id != measure.Code)
            {
                return NotFound();
            }
            ModelState.Remove(nameof(measure.Indicator));
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(measure);
                    await _context.SaveChangesAsync();
                    
                    // Update indicator performance
                    await _monitoringService.UpdateIndicatorPerformance(measure.IndicatorCode);
                    TempData["SuccessMessage"] = "Measure updated successfully and indicator performance has been updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeasureExists(measure.Code))
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
            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "IndicatorCode", "Name", measure.IndicatorCode);
            return View(measure);
        }




        // GET: Measures/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var measure = await _context.Measures
                .Include(m => m.Indicator)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (measure == null)
                return NotFound();

            return View(measure);
        }

        // POST: Measures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var monitoringService = new MonitoringService(_context);
            try
            {
                await monitoringService.DeleteMeasureAndRecalculateAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }



        //// GET: Measures/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var measure = await _context.Measures
        //        .Include(m => m.Indicator)
        //        .FirstOrDefaultAsync(m => m.Code == id);
        //    if (measure == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(measure);
        //}

        //// POST: Measures/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var measure = await _context.Measures.FindAsync(id);
        //    if (measure != null)
        //    {
        //        _context.Measures.Remove(measure);
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        private bool MeasureExists(int id)
        {
            return _context.Measures.Any(e => e.Code == id);
        }
    }
}
