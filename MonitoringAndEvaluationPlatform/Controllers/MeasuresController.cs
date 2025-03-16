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

        // GET: Measures/Create
        public IActionResult Create()
        {
            ViewData["Indicators"] = new SelectList(_context.Indicators, "IndicatorCode", "Name");
            ViewData["Projects"] = new SelectList(_context.Project, "ProjectID", "ProjectName");
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

            if (ModelState.IsValid)
            {
                _context.Add(measure);
                await _context.SaveChangesAsync();

                // Update IndicatorPerformance only if it's a "Real" measure
                if (measure.ValueType == MeasureValueType.Real)
                {
                    var monitoringService = new MonitoringService(_context);
                    await monitoringService.UpdateIndicatorPerformance(measure.IndicatorCode);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["Indicator"] = new SelectList(_context.Indicators, "IndicatorCode", "Name", measure.IndicatorCode);
            ViewData["Project"] = new SelectList(_context.Project, "ProjectID", "ProjectName", measure.ProjectID);

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
            ViewData["IndicatorCode"] = new SelectList(_context.Indicators, "Code", "Code", measure.IndicatorCode);
            return View(measure);
        }

        // POST: Measures/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Date,Value,ValueType,IndicatorCode")] Measure measure)
        {
            if (id != measure.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(measure);
                    await _context.SaveChangesAsync();
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

        // GET: Measures/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // POST: Measures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var measure = await _context.Measures.FindAsync(id);
            if (measure != null)
            {
                _context.Measures.Remove(measure);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MeasureExists(int id)
        {
            return _context.Measures.Any(e => e.Code == id);
        }
    }
}
