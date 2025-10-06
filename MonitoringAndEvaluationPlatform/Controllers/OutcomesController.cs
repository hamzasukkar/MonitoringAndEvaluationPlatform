using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class OutcomesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPerformanceService _performanceService;

        public OutcomesController(ApplicationDbContext context, IPerformanceService performanceService)
        {
            _context = context;
            _performanceService = performanceService;
        }

        // GET: Outcomes
        [Permission(Permissions.ReadOutcomes)]
        public async Task<IActionResult> Index(int? frameworkCode)
        {
            if (frameworkCode == null)
            {
                var applicationDbContext = _context.Outcomes.Include(o => o.Framework);
                return View(await applicationDbContext.ToListAsync());
            }

            ViewBag.SelectedFrameworkCode = frameworkCode; // Store it for use in the view

            var outcomes = await _context.Outcomes
              .Include(o => o.Framework).Include(x => x.Outputs)
              .Where(m => m.FrameworkCode==frameworkCode).ToListAsync();

            if (outcomes == null)
            {
                return NotFound();
            }

            return View(outcomes);
        }

        [HttpPost]
        [Permission(Permissions.ModifyOutcome)]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var outcome = await _context.Outcomes.FindAsync(id);
            if (outcome == null) return NotFound();

            outcome.Name = name;
            _context.Update(outcome);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Outcomes/Delete/5
        [HttpPost]
        [Permission(Permissions.DeleteOutcome)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var outcome = await _context.Outcomes.FindAsync(id);
            if (outcome == null) return NotFound();

            _context.Outcomes.Remove(outcome);
            await _context.SaveChangesAsync();
            return Ok();
        }

        private bool OutcomeExists(int id)
        {
            return _context.Outcomes.Any(e => e.Code == id);
        }

        // GET: FramworkOutcomes
        [Permission(Permissions.ReadOutcomes)]
        public async Task<IActionResult> FramworkOutcomes(int? id)
        {
            var applicationDbContext = _context.Outcomes.Where(m => m.FrameworkCode == id);
            ViewData["FrameworkName"] = _context.Frameworks.Where(i => i.Code == id).FirstOrDefault().Name;
            return View(await applicationDbContext.ToListAsync());
        }

        private async Task RedistributeWeights(int frameworkCode)
        {
            var outcomes = await _context.Outcomes
                .Where(i => i.FrameworkCode == frameworkCode)
                .ToListAsync();

            if (outcomes.Count == 0)
                return;

            double equalWeight = 100.0 / outcomes.Count;

            foreach (var i in outcomes)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = outcomes.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                outcomes.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

        [Permission(Permissions.ModifyOutcome)]
        public async Task<IActionResult> AdjustWeights(int frameworkCode)
        {
            var outcomes = await _context.Outcomes
                .Where(i => i.FrameworkCode == frameworkCode)
                .ToListAsync();

            var model = outcomes.Select(i => new OutcomesViewModel
            {
                Code = i.Code,
                Name = i.Name,
                Weight = i.Weight
            }).ToList();

            ViewBag.FrameworkCode = frameworkCode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyOutcome)]
        public async Task<IActionResult> AdjustWeights(List<OutcomesViewModel> model,int frameworkCode)
        {
            double totalWeight = model.Sum(i => i.Weight);

            if (Math.Abs(totalWeight - 100.0) > 0.01)
            {
                ModelState.AddModelError("", "Total weight must equal 100%.");
                ViewBag.FrameworkCode = frameworkCode;
                return View(model);
            }

            foreach (var vm in model)
            {
                var outcome = await _context.Outcomes.FindAsync(vm.Code);
                if (outcome != null)
                {
                    outcome.Weight = vm.Weight;
                    _context.Update(outcome);
                }
            }

            await _context.SaveChangesAsync();

            await _performanceService.UpdateFrameworkPerformance(frameworkCode);

            return RedirectToAction(nameof(Index), new { frameworkCode = frameworkCode });
        }
    }
}

