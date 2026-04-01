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

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class MeasuresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MonitoringService _monitoringService;
        private readonly IStringLocalizer<MeasuresController> _localizer;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public MeasuresController(ApplicationDbContext context, MonitoringService monitoringService, IStringLocalizer<MeasuresController> localizer, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _monitoringService = monitoringService;
            _localizer = localizer;
            _webHostEnvironment = webHostEnvironment;
        }

        // POST: add-measure (AJAX)
        [HttpPost("add-measure")]
        public async Task<IActionResult> AddMeasure([FromBody] AddMeasureDto dto)
        {
            var phase = await _context.ProjectPhases.FindAsync(dto.PhaseId);
            if (phase == null) return NotFound();

            // Auto-calculate Value from Quantity if phase has a TargetQuantity
            if (dto.Quantity.HasValue && phase.TargetQuantity.HasValue && phase.TargetQuantity.Value > 0)
            {
                dto.Value = Math.Min((dto.Quantity.Value / phase.TargetQuantity.Value) * 100.0, 100.0);
            }
            else
            {
                if (dto.Value <= 0 || dto.Value > 100)
                    return BadRequest(_localizer["Value must be between 0 and 100."]);
            }

            var measureType = dto.Quantity.HasValue ? MeasureType.Quantitative : MeasureType.Qualitative;

            // Enforce: all measures in a phase must share the same type
            var existingType = await _context.Measures
                .Where(m => m.ProjectPhaseId == dto.PhaseId)
                .Select(m => (MeasureType?)m.MeasureType)
                .FirstOrDefaultAsync();

            if (existingType.HasValue && existingType.Value != measureType)
                return BadRequest(_localizer["A phase cannot mix Qualitative and Quantitative measures."]);

            var existingTotal = await _context.Measures
                .Where(m => m.ProjectPhaseId == dto.PhaseId)
                .SumAsync(m => m.Value);

            if (existingTotal >= 100)
                return BadRequest(_localizer["Total measures value for this phase cannot exceed 100%."]);

            if (existingTotal + dto.Value > 100)
                return BadRequest(_localizer["Total measures value for this phase cannot exceed 100%."]);

            await _monitoringService.AddMeasureToPhase(dto.PhaseId, dto.Value, dto.Name, dto.Note, dto.Quantity, dto.Unit, measureType);
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
                    note = m.Note,
                    quantity = m.Quantity,
                    unit = m.Unit
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
            public double? Quantity { get; set; }
            public string? Unit { get; set; }
        }

        // GET: Measures
        public async Task<IActionResult> Index(int? phaseId)
        {
            var query = _context.Measures
                .Include(m => m.ProjectPhase)
                    .ThenInclude(pp => pp.Project)
                .Include(m => m.Files)
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

                // Pass the type locked by the first existing measure (null = no measures yet)
                ViewBag.ExistingPhaseType = await _context.Measures
                    .Where(m => m.ProjectPhaseId == phaseId.Value)
                    .OrderBy(m => m.Date)
                    .Select(m => (MeasureType?)m.MeasureType)
                    .FirstOrDefaultAsync();
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
        public async Task<IActionResult> CreateFromDetails(Measure measure, List<IFormFile>? MeasureFiles)
        {
            ModelState.Remove(nameof(measure.ProjectPhase));

            // Auto-calculate Value from Quantity if phase has a TargetQuantity
            var phase = await _context.ProjectPhases.FindAsync(measure.ProjectPhaseId);
            if (measure.Quantity.HasValue && phase?.TargetQuantity.HasValue == true && phase.TargetQuantity.Value > 0)
            {
                measure.Value = Math.Min((measure.Quantity.Value / phase.TargetQuantity.Value) * 100.0, 100.0);
                ModelState.Remove(nameof(measure.Value));
            }
            else
            {
                if (measure.Value <= 0 || measure.Value > 100)
                    return BadRequest(new { message = _localizer["Value must be between 0 and 100."].Value });
            }

            var existingTotal = await _context.Measures
                .Where(m => m.ProjectPhaseId == measure.ProjectPhaseId)
                .SumAsync(m => m.Value);

            if (existingTotal >= 100)
                return BadRequest(new { message = _localizer["Total measures value for this phase cannot exceed 100%."].Value });

            if (existingTotal + measure.Value > 100)
                return BadRequest(new { message = _localizer["Total measures value for this phase cannot exceed 100%."].Value });

            if (ModelState.IsValid)
            {
                _context.Add(measure);
                await _context.SaveChangesAsync();

                if (MeasureFiles != null && MeasureFiles.Count > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "measures");
                    Directory.CreateDirectory(uploadsFolder);
                    foreach (var file in MeasureFiles.Where(f => f.Length > 0))
                    {
                        var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueName);
                        using var fs = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(fs);
                        _context.MeasureFiles.Add(new Models.MeasureFile
                        {
                            MeasureCode = measure.Code,
                            FileName = file.FileName,
                            FilePath = $"/uploads/measures/{uniqueName}"
                        });
                    }
                    await _context.SaveChangesAsync();
                }

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

                // Pass the type locked by the first existing measure (null = no measures yet)
                ViewBag.ExistingPhaseType = await _context.Measures
                    .Where(m => m.ProjectPhaseId == phaseId.Value)
                    .OrderBy(m => m.Date)
                    .Select(m => (MeasureType?)m.MeasureType)
                    .FirstOrDefaultAsync();

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
        public async Task<IActionResult> Create([Bind("Code,Name,Date,Value,Note,Quantity,Unit,ProjectPhaseId")] Measure measure)
        {
            ModelState.Remove(nameof(measure.ProjectPhase));

            // Auto-calculate Value from Quantity if phase has a TargetQuantity
            var phase = await _context.ProjectPhases.FindAsync(measure.ProjectPhaseId);
            if (measure.Quantity.HasValue && phase?.TargetQuantity.HasValue == true && phase.TargetQuantity.Value > 0)
            {
                measure.Value = Math.Min((measure.Quantity.Value / phase.TargetQuantity.Value) * 100.0, 100.0);
                ModelState.Remove(nameof(measure.Value));
            }

            // Derive MeasureType from Quantity if not explicitly set
            if (measure.MeasureType == MeasureType.Qualitative && measure.Quantity.HasValue)
                measure.MeasureType = MeasureType.Quantitative;

            if (ModelState.IsValid)
            {
                // Enforce: all measures in a phase must share the same type
                var existingType = await _context.Measures
                    .Where(m => m.ProjectPhaseId == measure.ProjectPhaseId)
                    .Select(m => (MeasureType?)m.MeasureType)
                    .FirstOrDefaultAsync();

                if (existingType.HasValue && existingType.Value != measure.MeasureType)
                {
                    ModelState.AddModelError("MeasureType", _localizer["A phase cannot mix Qualitative and Quantitative measures."]);
                    ViewData["Phases"] = new SelectList(
                        _context.ProjectPhases.Include(pp => pp.Project),
                        "Id", "Name", measure.ProjectPhaseId);
                    return View(measure);
                }

                var existingTotal = await _context.Measures
                    .Where(m => m.ProjectPhaseId == measure.ProjectPhaseId)
                    .SumAsync(m => m.Value);

                if (existingTotal >= 100 || existingTotal + measure.Value > 100)
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
                existingMeasure.Quantity = measure.Quantity;
                existingMeasure.Unit = measure.Unit;

                var newType = measure.Quantity.HasValue ? MeasureType.Quantitative : MeasureType.Qualitative;

                // Enforce: all other measures in this phase must share the same type
                var conflictingType = await _context.Measures
                    .Where(m => m.ProjectPhaseId == existingMeasure.ProjectPhaseId && m.Code != id)
                    .Select(m => (MeasureType?)m.MeasureType)
                    .FirstOrDefaultAsync();

                if (conflictingType.HasValue && conflictingType.Value != newType)
                    return BadRequest(new { message = _localizer["A phase cannot mix Qualitative and Quantitative measures."].Value });

                existingMeasure.MeasureType = newType;

                // Auto-calculate Value from Quantity if phase has a TargetQuantity
                double computedValue;
                var phaseForEdit = await _context.ProjectPhases.FindAsync(existingMeasure.ProjectPhaseId);
                if (measure.Quantity.HasValue && phaseForEdit?.TargetQuantity.HasValue == true && phaseForEdit.TargetQuantity.Value > 0)
                {
                    computedValue = Math.Min((measure.Quantity.Value / phaseForEdit.TargetQuantity.Value) * 100.0, 100.0);
                }
                else
                {
                    computedValue = Math.Max(0, Math.Min(100, measure.Value));
                }

                var otherTotal = await _context.Measures
                    .Where(m => m.ProjectPhaseId == existingMeasure.ProjectPhaseId && m.Code != id)
                    .SumAsync(m => m.Value);

                if (otherTotal + computedValue > 100)
                    return BadRequest(new { message = _localizer["Total measures value for this phase cannot exceed 100%."].Value });

                existingMeasure.Value = computedValue;

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
