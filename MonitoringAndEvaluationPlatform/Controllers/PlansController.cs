using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using Microsoft.AspNetCore.Authorization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class PlanValueUpdate
    {
        public int PlanCode { get; set; }
        public string ValueType { get; set; }
        public long NewValue { get; set; }
    }

    [Authorize]
    public class PlansController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PlanService _planService;
        private readonly IMinistryScopeService _scope;
        private readonly ILogger<PlansController> _logger;

        public PlansController(ApplicationDbContext context, PlanService planService, IMinistryScopeService scope, ILogger<PlansController> logger)
        {
            _context = context;
            _planService = planService;
            _scope = scope;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePlanValues([FromBody] List<PlanValueUpdate> updates)
        {
            if (updates == null || !updates.Any())
            {
                return Json(new { success = false, message = "No updates provided." });
            }

            // EVERY plan code in the batch must be checked, not just one - otherwise a caller
            // could smuggle another ministry's plan codes in alongside their own.
            foreach (var update in updates)
            {
                if (!await _scope.PlanBelongsToScopeAsync(update.PlanCode))
                {
                    return Forbid();
                }
            }

            try
            {
                // Update all plans except the last one directly (no performance cascade)
                for (int i = 0; i < updates.Count - 1; i++)
                {
                    var update = updates[i];
                    var plan = await _context.Plans.FindAsync(update.PlanCode);
                    if (plan == null) continue;

                    if (update.ValueType == "Realised")
                        plan.Realised = update.NewValue;
                }
                await _context.SaveChangesAsync();

                // For the last update, use _planService.UpdatePlanAsync to trigger performance cascade
                var lastUpdate = updates.Last();
                var lastPlan = await _context.Plans.FindAsync(lastUpdate.PlanCode);
                if (lastPlan != null)
                {
                    if (lastUpdate.ValueType == "Realised")
                        lastPlan.Realised = lastUpdate.NewValue;

                    await _planService.UpdatePlanAsync(lastPlan);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update plan values.");
                return Json(new { success = false, message = "An internal server error occurred." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePlanValue(int planCode, string valueType, string newValue)
        {
            if (planCode <= 0)
            {
                return Json(new { success = false, message = "Invalid Plan Code." });
            }

            if (!long.TryParse(newValue, out long parsedValue))
            {
                return Json(new { success = false, message = "Invalid number. Please enter a whole number." });
            }

            // Without this check any authenticated user could rewrite any project's realised
            // disbursement simply by incrementing planCode.
            if (!await _scope.PlanBelongsToScopeAsync(planCode))
            {
                return Forbid();
            }

            try
            {
                // Find the Plan entity directly by its Primary Key (Code)
                var plan = await _context.Plans.FindAsync(planCode);

                if (plan == null)
                {
                    return Json(new { success = false, message = "The record could not be found." });
                }

                // Update the correct property based on valueType
                if (valueType == "Realised")
                {
                    plan.Realised = parsedValue;
                }
                else
                {
                    return Json(new { success = false, message = "Invalid data type specified." });
                }

                await _planService.UpdatePlanAsync(plan);

                // Return a success response
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update plan {PlanCode}.", planCode);
                return Json(new { success = false, message = "An internal server error occurred." });
            }
        }
        // GET: Plans
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Plans.Include(p => p.ActionPlan);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> ProjectPlans(int? id)
        {
            ViewBag.ProjectId = id;

            var applicationDbContext = _context.Plans.Include(p => p.ActionPlan);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Plans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!await _scope.PlanBelongsToScopeAsync(id.Value))
            {
                return Forbid();
            }

            var plan = await _context.Plans
                .Include(p => p.ActionPlan)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // GET: Plans/Create
        public IActionResult Create()
        {
            ViewData["ActionPlanCode"] = new SelectList(_context.ActionPlans, "Code", "Code");
            return View();
        }

        // POST: Plans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Date,Realised,ActionPlanCode")] Plan plan)
        {
            ModelState.Remove(nameof(plan.ActionPlan));

            // The parent action plan decides which ministry this new plan belongs to.
            if (!await _scope.ActionPlanBelongsToScopeAsync(plan.ActionPlanCode))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _context.Add(plan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ActionPlanCode"] = new SelectList(_context.ActionPlans, "Code", "Code", plan.ActionPlanCode);
            return View(plan);
        }

        // GET: Plans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!await _scope.PlanBelongsToScopeAsync(id.Value))
            {
                return Forbid();
            }

            var plan = await _context.Plans.FindAsync(id);
            if (plan == null)
            {
                return NotFound();
            }
            ViewData["ActionPlanCode"] = new SelectList(_context.ActionPlans, "Code", "Code", plan.ActionPlanCode);
            return View(plan);
        }

        // POST: Plans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Date,Realised,ActionPlanCode")] Plan plan)
        {
            if (id != plan.Code)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(plan.ActionPlan));

            // Check the plan as it exists in the database, not as posted - otherwise a caller
            // could point ActionPlanCode at their own ministry to get past the check.
            if (!await _scope.PlanBelongsToScopeAsync(id))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _planService.UpdatePlanAsync(plan);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlanExists(plan.Code))
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
            return View(plan);
        }


        // GET: Plans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!await _scope.PlanBelongsToScopeAsync(id.Value))
            {
                return Forbid();
            }

            var plan = await _context.Plans
                .Include(p => p.ActionPlan)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // POST: Plans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await _scope.PlanBelongsToScopeAsync(id))
            {
                return Forbid();
            }

            var plan = await _context.Plans.FindAsync(id);
            if (plan != null)
            {
                _context.Plans.Remove(plan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PlanExists(int id)
        {
            return _context.Plans.Any(e => e.Code == id);
        }
    }
}
