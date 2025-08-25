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
            await _monitoringService.AddMeasureToProject(dto.ProjectId, dto.IndicatorId, dto.Value, dto.ValueType);
            return Ok("Measure added and Indicator Performance updated");
        }

        // DTO
        public class AddMeasureDto
        {
            public int ProjectId { get; set; }
            public int IndicatorId { get; set; }
            public double Value { get; set; }
            public MeasureValueType ValueType { get; set; }
        }

        // GET: Measures
        public async Task<IActionResult> Index(int? id)
        {
            if (id == null)
            {
                var applicationDbContext = _context.Measures.Include(m => m.Indicator);
                return View(await applicationDbContext.ToListAsync());
            }
            var measures = _context.Measures.Where(m => m.ProjectID == id).Include(m => m.Indicator).ToListAsync();

            return View(await measures);
        }

        public async Task<IActionResult> ProjectMeasures(int? id)
        {
            if (id == null)
            {
                var applicationDbContext = _context.Measures.Include(m => m.Indicator);
                return View(await applicationDbContext.ToListAsync());
            }

            ViewBag.ProjectId = id;
            ViewBag.Indicators = await _context.Indicators
            .Where(i => /* Filter to project-related indicators, if needed */ true)
            .ToListAsync();
            var measures = _context.Measures.Where(m => m.ProjectID == id).Include(m => m.Indicator).ToListAsync();

            return View(await measures);
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
            ViewData["Projects"] = new SelectList(_context.Projects, "ProjectID", "ProjectName");
            return View();
        }

        // POST: Measures/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Date,Value,ValueType,IndicatorCode,ProjectID")] Measure measure)
        {
            ModelState.Remove(nameof(measure.Indicator));
            ModelState.Remove(nameof(measure.Project));

            bool targetExists = await _context.Measures
                    .AnyAsync(m => m.ProjectID == measure.ProjectID
                        && m.IndicatorCode == measure.IndicatorCode
                        && m.ValueType == MeasureValueType.Target);

            // Validate Target uniqueness
            if (measure.ValueType == MeasureValueType.Target)
            {

                if (targetExists)
                {
                    ModelState.AddModelError(
                        nameof(measure.ValueType),
                        "Only one Target measure is allowed per Project and Indicator.");
                }
            }
            else if (measure.ValueType == MeasureValueType.Real)
            {
                if (!targetExists)
                {
                    ModelState.AddModelError(
                        nameof(measure.ValueType),
                        "Add Target before Value");
                }
            }

            if (ModelState.IsValid)
            {
               
                _context.Measures.Add(measure);
                await _context.SaveChangesAsync();

                // Update IndicatorPerformance only if it's a "Real" measure
                if (measure.ValueType == MeasureValueType.Real)
                {
                    var monitoringService = new MonitoringService(_context);
                    await monitoringService.UpdateIndicatorPerformance(measure.IndicatorCode);
                    await monitoringService.UpdateMinistryPerformance(measure.ProjectID);
                    await monitoringService.UpdateProjectPerformance(measure.ProjectID);
                    await monitoringService.UpdateSectorPerformance(measure.ProjectID);
                    await monitoringService.UpdateDonorPerformance(measure.ProjectID);
                }

                return RedirectToAction("ProjectMeasures", new { id = measure.ProjectID });
            }

            ViewData["Indicators"] = new SelectList(_context.Indicators, "IndicatorCode", "Name", measure.IndicatorCode);
            ViewData["Projects"] = new SelectList(_context.Projects, "ProjectID", "ProjectName", measure.ProjectID);

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
            ViewData["Indicators"] = new SelectList(_context.Indicators, "IndicatorCode", "Name");
            ViewData["Projects"] = new SelectList(_context.Projects, "ProjectID", "ProjectName");
            return View(measure);
        }

        // POST: Measures/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,Measure measure)
        {
            if (id != measure.Code)
            {
                return NotFound();
            }
            ModelState.Remove(nameof(measure.Indicator));
            ModelState.Remove(nameof(measure.Project));
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(measure);
                    await _context.SaveChangesAsync();
                    var monitoringService = new MonitoringService(_context);
                    await monitoringService.UpdateIndicatorPerformance(measure.IndicatorCode);
                    await monitoringService.UpdateMinistryPerformance(measure.ProjectID);
                    await monitoringService.UpdateProjectPerformance(measure.ProjectID);
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
            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "Code", "Code", measure.IndicatorCode);
            return View(measure);
        }

        [HttpPost]
        public async Task<IActionResult> EditInline([FromBody] Measure updated)
        {
            var existing = await _context.Measures.FindAsync(updated.Code);
            if (existing == null)
                return NotFound();

            existing.Date = updated.Date;
            existing.Value = updated.Value;

            await _context.SaveChangesAsync();
            var monitoringService = new MonitoringService(_context);
            await monitoringService.UpdateIndicatorPerformance(updated.IndicatorCode);
            await monitoringService.UpdateMinistryPerformance(updated.ProjectID);
            await monitoringService.UpdateProjectPerformance(updated.ProjectID);

            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteInline(int id, int projectId, int indicatorCode)
        {
            var measure = await _context.Measures
                .FirstOrDefaultAsync(m => m.Code == id && m.ProjectID == projectId && m.IndicatorCode == indicatorCode);

            if (measure == null)
                return NotFound();

            _context.Measures.Remove(measure);
            await _context.SaveChangesAsync();
            var monitoringService = new MonitoringService(_context);
            await monitoringService.UpdateIndicatorPerformance(indicatorCode);
            await monitoringService.UpdateMinistryPerformance(projectId);
            await monitoringService.UpdateProjectPerformance(projectId);

            return Ok();
        }


        // GET: Measures/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var measure = await _context.Measures
                .Include(m => m.Indicator)
                .Include(m => m.Project)
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
