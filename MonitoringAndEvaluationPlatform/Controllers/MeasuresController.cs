using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
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

        // POST: add-measure (AJAX)
        [HttpPost("add-measure")]
        public async Task<IActionResult> AddMeasure([FromBody] AddMeasureDto dto)
        {
            if (dto.Value < 0 || dto.Value > 100)
                return BadRequest(_localizer["Value must be between 0 and 100."]);

            var existingTotal = await _context.Measures
                .Where(m => m.ProjectPhaseId == dto.PhaseId)
                .SumAsync(m => m.Value);

            if (existingTotal + dto.Value > 100)
                return BadRequest(_localizer["Total measures value for this phase cannot exceed 100%."]);

            await _monitoringService.AddMeasureToPhase(dto.PhaseId, dto.Value, dto.Name, dto.Note);
            return Ok(_localizer["Measure added and Phase Performance updated"]);
        }

        // GET: Measures by Phase (for chart)
        [HttpGet]
        public async Task<IActionResult> GetMeasuresByPhase(int phaseId)
        {
            var measures = await _context.Measures
                .Where(m => m.ProjectPhaseId == phaseId)
                .OrderBy(m => m.Date)
                .Select(m => new
                {
                    date = m.Date.ToString("yyyy-MM-dd"),
                    value = m.Value,
                    name = m.Name,
                    note = m.Note
                })
                .ToListAsync();

            return Ok(measures);
        }

        // DTO
        public class AddMeasureDto
        {
            public int PhaseId { get; set; }
            public double Value { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Note { get; set; }
        }

        // GET: Measures
        public async Task<IActionResult> Index(int? phaseId)
        {
            var query = _context.Measures
                .Include(m => m.ProjectPhase)
                    .ThenInclude(pp => pp.Project)
                .AsQueryable();

            if (phaseId.HasValue)
            {
                query = query.Where(m => m.ProjectPhaseId == phaseId.Value);

                var phase = await _context.ProjectPhases
                    .Include(pp => pp.Project)
                    .FirstOrDefaultAsync(pp => pp.Id == phaseId.Value);

                ViewBag.SelectedPhase = phase;
                ViewBag.SelectedPhaseId = phaseId.Value;
                ViewBag.SelectedProject = phase?.Project;
                ViewBag.SelectedProjectId = phase?.ProjectID;
            }

            ViewData["PhaseId"] = new SelectList(
                _context.ProjectPhases.Include(pp => pp.Project),
                "Id", "Name");

            return View(await query.ToListAsync());
        }

        // GET: Measures/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var measure = await _context.Measures
                .Include(m => m.ProjectPhase)
                    .ThenInclude(pp => pp.Project)
                .FirstOrDefaultAsync(m => m.Code == id);

            if (measure == null) return NotFound();

            return View(measure);
        }

        // POST: CreateFromDetails (AJAX inline creation)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromDetails(Measure measure)
        {
            ModelState.Remove(nameof(measure.ProjectPhase));

            if (measure.Value < 0 || measure.Value > 100)
                return BadRequest(new { message = _localizer["Value must be between 0 and 100."].Value });

            var existingTotal = await _context.Measures
                .Where(m => m.ProjectPhaseId == measure.ProjectPhaseId)
                .SumAsync(m => m.Value);

            if (existingTotal + measure.Value > 100)
                return BadRequest(new { message = _localizer["Total measures value for this phase cannot exceed 100%."].Value });

            if (ModelState.IsValid)
            {
                _context.Add(measure);
                await _context.SaveChangesAsync();

                await _monitoringService.UpdatePhasePerformance(measure.ProjectPhaseId);

                return Ok(new { message = _localizer["Measure added successfully and phase performance updated"].Value });
            }

            return BadRequest(new { message = _localizer["Invalid input"].Value });
        }

        // GET: Measures/Create
        public async Task<IActionResult> Create(int? phaseId)
        {
            if (phaseId.HasValue)
            {
                var phase = await _context.ProjectPhases
                    .Include(pp => pp.Project)
                    .FirstOrDefaultAsync(pp => pp.Id == phaseId.Value);

                ViewBag.SelectedPhase = phase;
                ViewBag.PreSelectedPhaseId = phaseId.Value;
                ViewBag.SelectedProject = phase?.Project;
                ViewBag.SelectedProjectId = phase?.ProjectID;

                ViewData["Phases"] = new SelectList(
                    _context.ProjectPhases.Include(pp => pp.Project),
                    "Id", "Name", phaseId.Value);
            }
            else
            {
                ViewData["Phases"] = new SelectList(
                    _context.ProjectPhases.Include(pp => pp.Project),
                    "Id", "Name");
            }

            return View();
        }

        // POST: Measures/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Date,Value,Note,ProjectPhaseId")] Measure measure)
        {
            ModelState.Remove(nameof(measure.ProjectPhase));

            if (ModelState.IsValid)
            {
                var existingTotal = await _context.Measures
                    .Where(m => m.ProjectPhaseId == measure.ProjectPhaseId)
                    .SumAsync(m => m.Value);

                if (existingTotal + measure.Value > 100)
                {
                    ModelState.AddModelError("Value", _localizer["Total measures value for this phase cannot exceed 100%."]);
                    ViewData["Phases"] = new SelectList(
                        _context.ProjectPhases.Include(pp => pp.Project),
                        "Id", "Name", measure.ProjectPhaseId);
                    return View(measure);
                }

                _context.Measures.Add(measure);
                await _context.SaveChangesAsync();

                await _monitoringService.UpdatePhasePerformance(measure.ProjectPhaseId);

                TempData["SuccessMessage"] = _localizer["Measure added successfully and phase performance has been updated."].Value;
                return RedirectToAction(nameof(Index), new { phaseId = measure.ProjectPhaseId });
            }

            ViewData["Phases"] = new SelectList(
                _context.ProjectPhases.Include(pp => pp.Project),
                "Id", "Name", measure.ProjectPhaseId);
            return View(measure);
        }

        // GET: Measures/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var measure = await _context.Measures.FindAsync(id);
            if (measure == null) return NotFound();

            ViewData["Phases"] = new SelectList(
                _context.ProjectPhases.Include(pp => pp.Project),
                "Id", "Name", measure.ProjectPhaseId);
            return View(measure);
        }

        // POST: Measures/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Measure measure)
        {
            // AJAX inline edit
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.ContentType?.Contains("application/x-www-form-urlencoded") == true)
            {
                var existingMeasure = await _context.Measures.FindAsync(id);
                if (existingMeasure == null) return NotFound();

                existingMeasure.Date = measure.Date;
                existingMeasure.Name = measure.Name;
                existingMeasure.Note = measure.Note;
                var clampedValue = Math.Max(0, Math.Min(100, measure.Value));

                var otherTotal = await _context.Measures
                    .Where(m => m.ProjectPhaseId == existingMeasure.ProjectPhaseId && m.Code != id)
                    .SumAsync(m => m.Value);

                if (otherTotal + clampedValue > 100)
                    return BadRequest(new { message = _localizer["Total measures value for this phase cannot exceed 100%."].Value });

                existingMeasure.Value = clampedValue;

                try
                {
                    _context.Update(existingMeasure);
                    await _context.SaveChangesAsync();

                    await _monitoringService.UpdatePhasePerformance(existingMeasure.ProjectPhaseId);

                    return Ok(new { message = _localizer["Measure updated successfully"].Value });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeasureExists(id)) return NotFound();
                    throw;
                }
            }

            // Regular form POST
            if (id != measure.Code) return NotFound();

            ModelState.Remove(nameof(measure.ProjectPhase));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(measure);
                    await _context.SaveChangesAsync();

                    await _monitoringService.UpdatePhasePerformance(measure.ProjectPhaseId);
                    TempData["SuccessMessage"] = _localizer["Measure updated successfully and phase performance has been updated."].Value;
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeasureExists(measure.Code)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["Phases"] = new SelectList(
                _context.ProjectPhases.Include(pp => pp.Project),
                "Id", "Name", measure.ProjectPhaseId);
            return View(measure);
        }

        // GET: Measures/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var measure = await _context.Measures
                .Include(m => m.ProjectPhase)
                    .ThenInclude(pp => pp.Project)
                .FirstOrDefaultAsync(m => m.Code == id);

            if (measure == null) return NotFound();

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

        private bool MeasureExists(int id)
        {
            return _context.Measures.Any(e => e.Code == id);
        }
    }
}
