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
        public async Task<IActionResult> Create([Bind("Code,Name,Trend,IndicatorsPerformance,SubOutputCode")] Indicator indicator)
        {
            if (ModelState.IsValid || true)
            {
                _context.Add(indicator);
                await _context.SaveChangesAsync();
                var subOutput = _context.SubOutputs.Where(i => i.Code == indicator.SubOutputCode).FirstOrDefault();
                int IndicatorPerformance = 0;
                var subOutputIndicators = _context.Indicators.Where(i => i.SubOutputCode == subOutput.Code).ToList();

                foreach (var item in subOutputIndicators)
                {
                    IndicatorPerformance += item.IndicatorsPerformance;
                }

                IndicatorPerformance = IndicatorPerformance / subOutputIndicators.Count;
                subOutput.IndicatorsPerformance = IndicatorPerformance;

                var output = _context.Outputs.Where(i => i.Code == subOutput.OutputCode).FirstOrDefault();
                var subOutputs = _context.SubOutputs.Where(i => i.OutputCode == output.Code).ToList();
                int outputIndicatorPerformance = 0;
                foreach (var item in subOutputs)
                {
                    outputIndicatorPerformance += item.IndicatorsPerformance;
                }
                output.IndicatorsPerformance = outputIndicatorPerformance / subOutputs.Count;

                var outcome = _context.Outcomes.Where(i => i.Code == output.OutcomeCode).FirstOrDefault();
                var outcomeOutputs = _context.Outputs.Where(i => i.OutcomeCode == outcome.Code).ToList();
                int outcomeIndicatorPerformance = 0;

                foreach (var item in outcomeOutputs)
                {
                    outcomeIndicatorPerformance += item.IndicatorsPerformance;
                }

                outcome.IndicatorsPerformance = outcomeIndicatorPerformance / outcomeOutputs.Count;

                var framework = _context.Freamework.Where(i => i.Code == outcome.Code).FirstOrDefault();
                var frameworkOucomes = _context.Outcomes.Where(i => i.FrameworkCode == framework.Code).ToList();
                int frameworkIndicatorPerformance = 0;

                foreach (var item in frameworkOucomes)
                {
                    frameworkIndicatorPerformance += item.IndicatorsPerformance;
                }

                framework.IndicatorsPerformance = frameworkIndicatorPerformance / frameworkOucomes.Count;



                _context.Update(outcome);
                _context.Update(output);
                _context.Update(subOutput);
                _context.Update(framework);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
            return View(indicator);
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

        // POST: Indicators/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Trend,IndicatorsPerformance,SubOutputCode")] Indicator indicator)
        {
            if (id != indicator.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid || true)
            {
                try
                {
                    _context.Update(indicator);
                    var subOutput = _context.SubOutputs.Where(i => i.Code == indicator.SubOutputCode).FirstOrDefault();
                    int subOutputIndicatorPerformance = 0;
                    var subOutputIndicators = _context.Indicators.Where(i => i.SubOutputCode == subOutput.Code).ToList();

                    foreach (var item in subOutputIndicators)
                    {
                        subOutputIndicatorPerformance += item.IndicatorsPerformance;
                    }

                    subOutputIndicatorPerformance = subOutputIndicatorPerformance / subOutputIndicators.Count;
                    subOutput.IndicatorsPerformance = subOutputIndicatorPerformance;

                    _context.Update(subOutput);

                    var output = _context.Outputs.Where(i => i.Code == subOutput.OutputCode).FirstOrDefault();
                    var subOutputs = _context.SubOutputs.Where(i => i.OutputCode == output.Code).ToList();
                    int outputIndicatorPerformance = 0;
                    foreach (var item in subOutputs)
                    {
                        outputIndicatorPerformance += item.IndicatorsPerformance;
                    }
                    output.IndicatorsPerformance = outputIndicatorPerformance / subOutputs.Count;
                    _context.Update(output);

                    var outcome = _context.Outcomes.Where(i => i.Code == output.OutcomeCode).FirstOrDefault();
                    var outcomeOutputs = _context.Outputs.Where(i => i.OutcomeCode == outcome.Code).ToList();
                    int outcomeIndicatorPerformance = 0;

                    foreach (var item in outcomeOutputs)
                    {
                        outcomeIndicatorPerformance += item.IndicatorsPerformance;
                    }

                    outcome.IndicatorsPerformance = outcomeIndicatorPerformance / outcomeOutputs.Count;

                    var framework = _context.Freamework.Where(i => i.Code == outcome.Code).FirstOrDefault();
                    var frameworkOucomes = _context.Outcomes.Where(i => i.FrameworkCode == framework.Code).ToList();
                    int frameworkIndicatorPerformance = 0;

                    foreach (var item in frameworkOucomes)
                    {
                        frameworkIndicatorPerformance += item.IndicatorsPerformance;
                    }

                    framework.IndicatorsPerformance = frameworkIndicatorPerformance / frameworkOucomes.Count;

                    _context.Update(framework);

                    await _context.SaveChangesAsync();
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
    }
}
