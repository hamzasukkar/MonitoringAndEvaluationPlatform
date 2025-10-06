using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class SubOutputsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPerformanceService _performanceService;

        public SubOutputsController(ApplicationDbContext context, IPerformanceService performanceService)
        {
            _context = context;
            _performanceService = performanceService;
        }

        // GET: SubOutputs
        [Permission(Permissions.ReadSubOutputs)]
        public async Task<IActionResult> Index(int? frameworkCode, int? outputCode)
        {
            IQueryable<SubOutput> query = _context.SubOutputs
                .Include(s => s.Output)
                .Include(s => s.Indicators)
                .Include(s => s.Output.Outcome.Framework);

            if (frameworkCode != null)
            {
                // Filter by frameworkCode
                query = query.Where(s => s.Output.Outcome.FrameworkCode == frameworkCode);
                ViewBag.SelectedFrameworkCode = frameworkCode; // Store for view
            }
            else if (outputCode != null)
            {
                // Filter by outputCode
                query = query.Where(s => s.OutputCode == outputCode);
                ViewBag.SelectedOutputCode = outputCode; // Store for view
            }
            // If both are null, we'll return all records

            var subOutputs = await query.ToListAsync();

            if (subOutputs == null)
            {
                return NotFound();
            }

            // Set ViewData for breadcrumb
            ViewData["frameworkCode"] = frameworkCode;
            ViewData["outputCode"] = outputCode;

            return View(subOutputs);
        }

        [HttpPost]
        [Permission(Permissions.ModifySubOutput)]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var subOutput = await _context.SubOutputs.FindAsync(id);

            if (subOutput == null) return NotFound();

            subOutput.Name = name;
            _context.Update(subOutput);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: SubOutputs/Delete/5
        [HttpPost]
        [Permission(Permissions.DeleteSubOutput)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subOutput = await _context.SubOutputs.FindAsync(id);

            if (subOutput == null) return NotFound();

            _context.SubOutputs.Remove(subOutput);

            await _context.SaveChangesAsync();
            return Ok();
        }

        private bool SubOutputExists(int id)
        {
            return _context.SubOutputs.Any(e => e.Code == id);
        }


        private async Task RedistributeWeights(int outputCode)
        {
            var subOutputs = await _context.SubOutputs
                .Where(i => i.OutputCode == outputCode)
                .ToListAsync();

            if (subOutputs.Count == 0)
                return;

            double equalWeight = 100.0 / subOutputs.Count;

            foreach (var i in subOutputs)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = subOutputs.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                subOutputs.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

        // GET: Indicators/AdjustWeights/5
        [Permission(Permissions.ModifySubOutput)]
        public async Task<IActionResult> AdjustWeights(int outputCode)
        {
            var subOutputs = await _context.SubOutputs
                .Where(i => i.OutputCode == outputCode)
                .ToListAsync();

            var model = subOutputs.Select(i => new SubOutputViewModel
            {
                Code = i.Code,
                Name = i.Name,
                Weight = i.Weight
            }).ToList();

            ViewBag.OutputCode = outputCode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifySubOutput)]
        public async Task<IActionResult> AdjustWeights(List<SubOutputViewModel> model, int outputCode)
        {
            double totalWeight = model.Sum(i => i.Weight);

            if (Math.Abs(totalWeight - 100.0) > 0.01)
            {
                ModelState.AddModelError("", "Total weight must equal 100%.");
                ViewBag.OutputCode = outputCode;
                return View(model);
            }

            foreach (var vm in model)
            {
                var subOutput = await _context.SubOutputs.FindAsync(vm.Code);
                if (subOutput != null)
                {
                    subOutput.Weight = vm.Weight;
                    _context.Update(subOutput);
                }
            }

            await _context.SaveChangesAsync();

            await _performanceService.UpdateOutputPerformance(outputCode);


            return RedirectToAction(nameof(Index), new { outputCode = outputCode });
        }

       


    }

}
