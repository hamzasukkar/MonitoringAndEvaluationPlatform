using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class IndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IndicatorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Indicators
        public async Task<IActionResult> Index(int? id, string searchString)
        {
            if (id == null)
            {
                // Include the SubOutput navigation property
                var indicators = _context.Indicators.Include(i => i.SubOutput).AsQueryable();

                // Filter results if searchString is provided
                if (!string.IsNullOrEmpty(searchString))
                {
                    indicators = indicators.Where(i => i.Name.Contains(searchString) ||
                                                       (i.SubOutput != null && i.SubOutput.Name.Contains(searchString)));
                }

                return View(await indicators.ToListAsync());
            }

            var frameworkIndicators = _context.Indicators.Where(i => i.SubOutput.Output.Outcome.FrameworkCode == id).Include(i => i.SubOutput).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                frameworkIndicators = frameworkIndicators.Where(i => i.Name.Contains(searchString) ||
                                                   (i.SubOutput != null && i.SubOutput.Name.Contains(searchString)));
            }

            return View(await frameworkIndicators.ToListAsync());
        }





        // GET: Indicators/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var indicator = await _context.Indicators
        //        .Include(i => i.SubOutput)
        //        .FirstOrDefaultAsync(m => m.Code == id);
        //    if (indicator == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(indicator);
        //}

        // GET: Indicators/Create
        public IActionResult Create()
        {
            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name");
            return View();
        }

        // POST: Indicators/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Trend,IndicatorsPerformance,SubOutputCode,Weight")] Indicator indicator)
        {
            ModelState.Remove(nameof(indicator.SubOutput));

            if (ModelState.IsValid)
            {
                // Add the new indicator
                _context.Add(indicator);
                await _context.SaveChangesAsync();

                // Update related entities
                await UpdateSubOutputPerformance(indicator.SubOutputCode);

                return RedirectToAction(nameof(Index));
            }

            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
            return View(indicator);
        }

        private async Task UpdateSubOutputPerformance(int subOutputCode)
        {
            var subOutput = await _context.SubOutputs.FirstOrDefaultAsync(i => i.Code == subOutputCode);

            if (subOutput == null) return;

            var indicators = await _context.Indicators.Where(i => i.SubOutputCode == subOutput.Code).ToListAsync();
            subOutput.IndicatorsPerformance = CalculateAveragePerformance(indicators.Select(i => i.IndicatorsPerformance).ToList());

            await _context.SaveChangesAsync();

            await UpdateOutputPerformance(subOutput.OutputCode);
        }

        private async Task UpdateOutputPerformance(int outputCode)
        {
            var output = await _context.Outputs.FirstOrDefaultAsync(i => i.Code == outputCode);

            if (output == null) return;

            var subOutputs = await _context.SubOutputs.Where(i => i.OutputCode == output.Code).ToListAsync();
            output.IndicatorsPerformance = CalculateAveragePerformance(subOutputs.Select(s => s.IndicatorsPerformance).ToList());

            await _context.SaveChangesAsync();

            await UpdateOutcomePerformance(output.OutcomeCode);
        }

        private async Task UpdateOutcomePerformance(int outcomeCode)
        {
            var outcome = await _context.Outcomes.FirstOrDefaultAsync(i => i.Code == outcomeCode);

            if (outcome == null) return;

            var outputs = await _context.Outputs.Where(i => i.OutcomeCode == outcome.Code).ToListAsync();
            outcome.IndicatorsPerformance = CalculateAveragePerformance(outputs.Select(o => o.IndicatorsPerformance).ToList());

            await _context.SaveChangesAsync();

            await UpdateFrameworkPerformance(outcome.FrameworkCode);
        }

        private async Task UpdateFrameworkPerformance(int frameworkCode)
        {
            var framework = await _context.Framework.FirstOrDefaultAsync(i => i.Code == frameworkCode);

            if (framework == null) return;

            var outcomes = await _context.Outcomes.Where(i => i.FrameworkCode == framework.Code).ToListAsync();
            framework.IndicatorsPerformance = CalculateAveragePerformance(outcomes.Select(o => o.IndicatorsPerformance).ToList());

            await _context.SaveChangesAsync();
        }

        private int CalculateAveragePerformance(List<int> performances)
        {
            return performances.Any() ? performances.Sum() / performances.Count : 0;
        }


        // GET: Indicators/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indicator = await _context.Indicators.FindAsync(id);
            if (indicator == null)
            {
                return NotFound();
            }
            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
            return View(indicator);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Trend,IndicatorsPerformance,SubOutputCode")] Indicator indicator)
        {
            if (id != indicator.Code)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(indicator.SubOutput));

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the indicator
                    _context.Update(indicator);
                    await _context.SaveChangesAsync();

                    // Update related entities
                    await UpdateSubOutputPerformance(indicator.SubOutputCode);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IndicatorExists(indicator.Code))
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

            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
            return View(indicator);
        }

        // GET: Indicators/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indicator = await _context.Indicators
                .Include(i => i.SubOutput)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (indicator == null)
            {
                return NotFound();
            }

            return View(indicator);
        }
        public async Task<IActionResult> Details(int id)
        {
            var indicator = await _context.Indicators
                .Include(i => i.SubOutput)
                .ThenInclude(so => so.Output)
                .ThenInclude(o => o.Outcome)
                .ThenInclude(oc => oc.Framework)
                .FirstOrDefaultAsync(i => i.Code == id);

            if (indicator == null)
                return NotFound();

            // Build the hierarchy model
            var hierarchy = new List<(string Name, string Type, int Code)>
    {
        (indicator.SubOutput.Output.Outcome.Framework.Name, "Framework", indicator.SubOutput.Output.Outcome.Framework.Code),
        (indicator.SubOutput.Output.Outcome.Name, "Outcome", indicator.SubOutput.Output.Outcome.Code),
        (indicator.SubOutput.Output.Name, "Output", indicator.SubOutput.Output.Code),
        (indicator.SubOutput.Name, "SubOutput", indicator.SubOutput.Code),
        (indicator.Name, "Indicator", indicator.Code)
    };

            var model = new IndicatorDetailsViewModel
            {
                Indicator = indicator,
                Hierarchy = hierarchy
            };

            return View(model);
        }




        // POST: Indicators/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var indicator = await _context.Indicators.FindAsync(id);
            if (indicator != null)
            {
                _context.Indicators.Remove(indicator);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IndicatorExists(int id)
        {
            return _context.Indicators.Any(e => e.Code == id);
        }

        public IActionResult Chart()
        {
                var viewModel = new ChartDataViewModel
                {
                    Labels = new List<string>
            {
                "2019-01-01", "2022-01-01", "2030-02-01", "2030-03-01",
                "2030-04-01", "2030-05-01", "2030-06-01"
            },
                    RealData = new List<double> { 80, 85, 90, 95, 98, 99, 100 },
                    HistoricalData = new List<double> { 80, 82, 83, 85, 87, 89, 91 },
                    RequiredData = new List<double> { 80, 83, 86, 89, 92, 95, 100 }
                };

                return View(viewModel);
        }


    }
}
