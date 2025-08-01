﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class LogicalFrameworkIndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogicalFrameworkIndicatorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LogicalFrameworkIndicators
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.logicalFrameworkIndicators.Include(l => l.LogicalFramework);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: LogicalFrameworkIndicators/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFrameworkIndicator = await _context.logicalFrameworkIndicators
                .Include(l => l.LogicalFramework)
                .FirstOrDefaultAsync(m => m.IndicatorCode == id);

            var logicalMeasures = await _context.logicalMeasures.Where(m => m.LogicalFrameworkIndicatorIndicatorCode == id).ToListAsync();

            var labels = new List<string>();
            var realData = new List<double>();
            var historicalData = new List<double>();
            var requiredData = new List<double>();

            foreach (var measure in logicalMeasures)
            {
                labels.Add(measure.Date.ToString());
                realData.Add(measure.Value);
                historicalData.Add(measure.Value + 20);
                requiredData.Add(measure.Value + 10);
            }

            var chartDataViewModel = new ChartDataViewModel
            {
                Labels = labels,
                RealData = realData,
                HistoricalData = historicalData,
                RequiredData = requiredData
            };

            var viewModel = new LogicalFrameworkIndicatorDetailsViewModel
            {
                LogicalFrameworkIndicator = logicalFrameworkIndicator,
                logicalMeasures = logicalFrameworkIndicator.logicalMeasures.OrderBy(m => m.Date).ToList(),
                ChartDataViewModel = chartDataViewModel, // your existing chart logic
                NewLogicalMeasure = new LogicalMeasure
                {
                    LogicalFrameworkIndicatorIndicatorCode = logicalFrameworkIndicator.IndicatorCode // ✅ critical!
                }
            };

            return View(viewModel);


            if (logicalFrameworkIndicator == null)
            {
                return NotFound();
            }

            return View(logicalFrameworkIndicator);
        }

        [HttpGet]
        public async Task<IActionResult> GetLogicalMeasureChartData(int indicatorCode)
        {
            var data = await _context.logicalMeasures
                .Where(m => m.LogicalFrameworkIndicatorIndicatorCode == indicatorCode)
                .OrderBy(m => m.Date)
                .ToListAsync();

            var real = data
                .Where(m => m.ValueType == MeasureValueType.Real)
                .Select(m => new { date = m.Date.ToString("yyyy-MM-dd"), value = m.Value })
                .ToList();

            var target = data
                .Where(m => m.ValueType == MeasureValueType.Target)
                .Select(m => new { date = m.Date.ToString("yyyy-MM-dd"), value = m.Value })
                .ToList();



            var result = new { Real = real, Target = target };

            return Json(result);
        }

        // GET: LogicalFrameworkIndicators/Create
        public IActionResult Create(int logicalFrameworkCode)
        {
            var logicalFramework = _context.logicalFrameworks.FirstOrDefault(lf => lf.Code == logicalFrameworkCode);
            if (logicalFramework == null)
            {
                return NotFound();
            }

            ViewBag.LogicalFrameworkName = logicalFramework.Name;
            ViewBag.LogicalFrameworkCode = logicalFrameworkCode;

            var relatedIndicators = _context.logicalFrameworkIndicators
                .Where(ind => ind.LogicalFrameworkCode == logicalFrameworkCode)
                .ToList();

            ViewBag.RelatedIndicators = relatedIndicators;

            var model = new LogicalFrameworkIndicator
            {
                LogicalFrameworkCode = logicalFrameworkCode
            };

            return View(model);
        }


        // POST: LogicalFrameworkIndicators/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(LogicalFrameworkIndicator indicator)
        {
            if (ModelState.IsValid || true)
            {
                _context.Add(indicator);
                _context.SaveChanges();

                TempData["Success"] = "Indicator created successfully!";

                // Stay on the same page and reload related indicators
                return RedirectToAction("Create", new { logicalFrameworkCode = indicator.LogicalFrameworkCode });
            }

            // If model state failed, re-fetch data to keep page functional
            var logicalFramework = _context.logicalFrameworks.FirstOrDefault(lf => lf.Code == indicator.LogicalFrameworkCode);
            ViewBag.LogicalFrameworkName = logicalFramework?.Name ?? "";
            ViewBag.LogicalFrameworkCode = indicator.LogicalFrameworkCode;
            ViewBag.RelatedIndicators = _context.logicalFrameworkIndicators
                .Where(ind => ind.IndicatorCode == indicator.LogicalFrameworkCode)
                .ToList();

            return View(indicator);
        }


        // GET: LogicalFrameworkIndicators/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFrameworkIndicator = await _context.logicalFrameworkIndicators.FindAsync(id);
            if (logicalFrameworkIndicator == null)
            {
                return NotFound();
            }
            ViewData["LogicalFrameworkCode"] = new SelectList(_context.logicalFrameworks, "Code", "Code", logicalFrameworkIndicator.LogicalFrameworkCode);
            return View(logicalFrameworkIndicator);
        }

        [HttpPost]
        public async Task<IActionResult> EditInline(int id, string name, int target)
        {
            var indicator = await _context.logicalFrameworkIndicators.FindAsync(id);
            if (indicator == null)
                return Json(new { success = false, message = "Indicator not found." });

            indicator.Name = name;
            indicator.Target = target;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: LogicalFrameworkIndicators/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Source,Performance,Weight,LogicalFrameworkCode,IsCommon,Active,Target,TargetYear,GAGRA,GAGRR,Concept,Description,MethodOfComputation,Comment")] LogicalFrameworkIndicator logicalFrameworkIndicator)
        {
            if (id != logicalFrameworkIndicator.IndicatorCode)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(logicalFrameworkIndicator);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LogicalFrameworkIndicatorExists(logicalFrameworkIndicator.IndicatorCode))
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
            ViewData["LogicalFrameworkCode"] = new SelectList(_context.logicalFrameworks, "Code", "Code", logicalFrameworkIndicator.LogicalFrameworkCode);
            return View(logicalFrameworkIndicator);
        }

        // GET: LogicalFrameworkIndicators/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logicalFrameworkIndicator = await _context.logicalFrameworkIndicators
                .Include(l => l.LogicalFramework)
                .FirstOrDefaultAsync(m => m.IndicatorCode == id);
            if (logicalFrameworkIndicator == null)
            {
                return NotFound();
            }

            return View(logicalFrameworkIndicator);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var indicator = await _context.logicalFrameworkIndicators.FindAsync(id);
            if (indicator == null)
                return Json(new { success = false, message = "Indicator not found." });

            _context.logicalFrameworkIndicators.Remove(indicator);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        private bool LogicalFrameworkIndicatorExists(int id)
        {
            return _context.logicalFrameworkIndicators.Any(e => e.IndicatorCode == id);
        }

        [HttpPost]
        public async Task<IActionResult> AddLogicalMeasure(LogicalMeasure measure)
        {

            ModelState.Remove(nameof(measure.LogicalFrameworkIndicator));

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.logicalMeasures.Add(measure);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public IActionResult GetLogicalMeasureTable(int indicatorCode)
        {
            var measures = _context.logicalMeasures
                .Where(m => m.LogicalFrameworkIndicatorIndicatorCode == indicatorCode)
                .OrderBy(m => m.Date)
                .ToList();

            return PartialView("_LogicalMeasureTableBody", measures);
        }
        public async Task<IActionResult> LogicalMeasureTablePartial(int indicatorCode)
        {
            var measures = await _context.logicalMeasures
                .Where(m => m.LogicalFrameworkIndicatorIndicatorCode == indicatorCode)
                .OrderBy(m => m.Date)
                .ToListAsync();

            return PartialView("_LogicalMeasureTablePartial", measures);
        }

    }
}
