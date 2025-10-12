using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        private readonly IStringLocalizer<MeasuresController> _localizer;

        public MeasuresController(ApplicationDbContext context, MonitoringService monitoringService, IStringLocalizer<MeasuresController> localizer)
        {
            _context = context;
            _monitoringService = monitoringService;
            _localizer = localizer;
        }

        [HttpPost("add-measure")]
        public async Task<IActionResult> AddMeasure([FromBody] AddMeasureDto dto)
        {
            await _monitoringService.AddMeasureToIndicator(dto.IndicatorId, dto.Value);
            return Ok(_localizer["Measure added and Indicator Performance updated"]);
        }

        // GET: Measures by Indicator (for chart)
        [HttpGet]
        public async Task<IActionResult> GetMeasuresByIndicator(int indicatorCode)
        {
            var measures = await _context.Measures
                .Where(m => m.IndicatorCode == indicatorCode)
                .OrderBy(m => m.Date)
                .Select(m => new
                {
                    date = m.Date.ToString("yyyy-MM-dd"),
                    value = m.Value
                })
                .ToListAsync();

            return Ok(measures);
        }

        // DTO
        public class AddMeasureDto
        {
            public int IndicatorId { get; set; }
            public double Value { get; set; }
        }

        // GET: Measures
        public async Task<IActionResult> Index(int? indicatorId)
        {
            var query = _context.Measures.Include(m => m.Indicator).AsQueryable();
            
            // Filter by indicator if provided
            if (indicatorId.HasValue)
            {
                query = query.Where(m => m.IndicatorCode == indicatorId.Value);
                
                // Pass indicator info to view for creating new measures
                var indicator = await _context.Indicators.FindAsync(indicatorId.Value);
                ViewBag.SelectedIndicator = indicator;
                ViewBag.SelectedIndicatorId = indicatorId.Value;
                
                // Find project associated with this indicator
                var projectIndicator = await _context.ProjectIndicators
                    .Include(pi => pi.Project)
                    .FirstOrDefaultAsync(pi => pi.IndicatorCode == indicatorId.Value);
                
                if (projectIndicator != null)
                {
                    ViewBag.SelectedProject = projectIndicator.Project;
                    ViewBag.SelectedProjectId = projectIndicator.ProjectId;
                }
            }
            
            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "IndicatorCode", "Name");
            
            return View(await query.ToListAsync());
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromDetails(Measure measure)
        {
            ModelState.Remove(nameof(measure.Indicator));

            if (ModelState.IsValid)
            {
                _context.Add(measure);
                await _context.SaveChangesAsync();

                // Update indicator performance after adding the measure
                await _monitoringService.UpdateIndicatorPerformance(measure.IndicatorCode);

                return Ok(new { message = _localizer["Measure added successfully and indicator performance updated"] });
            }

            return BadRequest(_localizer["Invalid input"]);
        }

        // GET: Measures/Create
        public async Task<IActionResult> Create(int? indicatorId)
        {
            if (indicatorId.HasValue)
            {
                // Pre-populate the form with the selected indicator
                var indicator = await _context.Indicators.FindAsync(indicatorId.Value);
                ViewBag.SelectedIndicator = indicator;
                ViewBag.PreSelectedIndicatorId = indicatorId.Value;
                
                // Set dropdown with the selected indicator highlighted
                ViewData["Indicators"] = new SelectList(_context.Indicators, "IndicatorCode", "Name", indicatorId.Value);
                
                // Find associated project for breadcrumb/navigation context
                var projectIndicator = await _context.ProjectIndicators
                    .Include(pi => pi.Project)
                    .FirstOrDefaultAsync(pi => pi.IndicatorCode == indicatorId.Value);
                
                if (projectIndicator != null)
                {
                    ViewBag.SelectedProject = projectIndicator.Project;
                    ViewBag.SelectedProjectId = projectIndicator.ProjectId;
                }
            }
            else
            {
                ViewData["Indicators"] = new SelectList(_context.Indicators, "IndicatorCode", "Name");
            }
            
            return View();
        }

        // POST: Measures/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Date,Value,IndicatorCode")] Measure measure)
        {
            ModelState.Remove(nameof(measure.Indicator));

            if (ModelState.IsValid)
            {
                _context.Measures.Add(measure);
                await _context.SaveChangesAsync();

                // Update indicator performance after adding the measure
                await _monitoringService.UpdateIndicatorPerformance(measure.IndicatorCode);

                TempData["SuccessMessage"] = _localizer["Measure added successfully and indicator performance has been updated."];

                // Redirect back to Index with indicator filter to show the measures for this indicator
                return RedirectToAction(nameof(Index), new { indicatorId = measure.IndicatorCode });
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
            // Check if this is an AJAX request (inline edit)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType?.Contains("application/x-www-form-urlencoded") == true)
            {
                // Get the existing measure from database
                var existingMeasure = await _context.Measures.FindAsync(id);
                if (existingMeasure == null)
                {
                    return NotFound();
                }

                // Update only the editable fields
                existingMeasure.Date = measure.Date;
                existingMeasure.Value = measure.Value;

                try
                {
                    _context.Update(existingMeasure);
                    await _context.SaveChangesAsync();

                    // Update indicator performance
                    await _monitoringService.UpdateIndicatorPerformance(existingMeasure.IndicatorCode);

                    return Ok(new { message = _localizer["Measure updated successfully"] });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeasureExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Regular form POST (from Edit view)
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
                    TempData["SuccessMessage"] = _localizer["Measure updated successfully and indicator performance has been updated."];
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

                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { message = _localizer["Measure deleted successfully"] });
                }
            }
            catch (InvalidOperationException ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return NotFound(new { message = ex.Message });
                }
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
