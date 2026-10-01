using Microsoft.AspNetCore.Authorization;
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

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class PlanValueUpdate
    {
        public int PlanCode { get; set; }
        public string ValueType { get; set; }
        public long NewValue { get; set; }
    }

    // Login required: nothing here is public. No fallback policy exists, so without this
    // attribute every action was reachable anonymously.
    //
    // Every action checks that the plan belongs to one of the caller's own projects. Which ROLES
    // may enter plan values is a separate, still-open decision, so no role-restricting
    // [Permission] attribute is applied here.
    [Authorize]
    public class PlansController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PlanService _planService;
        private readonly IMinistryScopeService _ministryScope;

        public PlansController(ApplicationDbContext context, PlanService planService, IMinistryScopeService ministryScope)
        {
            _context = context;
            _planService = planService;
            _ministryScope = ministryScope;
        }

        private async Task<SelectList> ActionPlanOptionsAsync(int? selected = null) =>
            new SelectList(
                await _context.ActionPlans.WithinScope(await _ministryScope.GetScopeAsync()).ToListAsync(),
                "Code", "Code", selected);

        [HttpPost]
        public async Task<IActionResult> UpdatePlanValues([FromBody] List<PlanValueUpdate> updates)
        {
            if (updates == null || !updates.Any())
            {
                return Json(new { success = false, message = "No updates provided." });
            }

            // All-or-nothing: one plan outside the caller's ministry rejects the whole batch.
            if (!await _ministryScope.CanAccessPlansAsync(updates.Select(u => u.PlanCode).ToList()))
            {
                return Forbid();
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
                return Json(new { success = false, message = "An internal server error occurred: " + ex.Message });
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

            if (!await _ministryScope.CanAccessPlansAsync(new[] { planCode }))
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
                return Json(new { success = false, message = "An internal server error occurred: " + ex.Message });
            }
        }
        // GET: Plans
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Plans
                .WithinScope(await _ministryScope.GetScopeAsync())
                .Include(p => p.ActionPlan)
                    .ThenInclude(ap => ap.ProjectPhase)
                        .ThenInclude(ph => ph.Project);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> ProjectPlans(int? id)
        {
            if (id.HasValue && !await _ministryScope.CanAccessProjectAsync(id.Value))
            {
                return Forbid();
            }

            ViewBag.ProjectId = id;

            // The project's plans when one is given; otherwise every plan the caller may see.
            var applicationDbContext = _context.Plans
                .WithinScope(await _ministryScope.GetScopeAsync())
                .Where(p => !id.HasValue || p.ActionPlan.ProjectPhase.ProjectID == id.Value)
                .Include(p => p.ActionPlan)
                    .ThenInclude(ap => ap.ProjectPhase)
                        .ThenInclude(ph => ph.Project);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Plans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.Plans
                .Include(p => p.ActionPlan)
                    .ThenInclude(ap => ap.ProjectPhase)
                        .ThenInclude(ph => ph.Project)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (plan == null)
            {
                return NotFound();
            }
            if (!await _ministryScope.CanAccessPlansAsync(new[] { plan.Code }))
            {
                return Forbid();
            }

            return View(plan);
        }

        // GET: Plans/Create
        public async Task<IActionResult> Create()
        {
            ViewData["ActionPlanCode"] = await ActionPlanOptionsAsync();
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

            if (!await _ministryScope.CanAccessActionPlanAsync(plan.ActionPlanCode))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _context.Add(plan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ActionPlanCode"] = await ActionPlanOptionsAsync(plan.ActionPlanCode);
            return View(plan);
        }

        // GET: Plans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.Plans.FindAsync(id);
            if (plan == null)
            {
                return NotFound();
            }
            if (!await _ministryScope.CanAccessPlansAsync(new[] { plan.Code }))
            {
                return Forbid();
            }
            ViewData["ActionPlanCode"] = await ActionPlanOptionsAsync(plan.ActionPlanCode);
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

            // The form binds ActionPlanCode, so it could also move the plan into another
            // ministry's action plan.
            if (!await _ministryScope.CanAccessPlansAsync(new[] { id })
                || !await _ministryScope.CanAccessActionPlanAsync(plan.ActionPlanCode))
            {
                return Forbid();
            }

            ModelState.Remove(nameof(plan.ActionPlan));
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

            var plan = await _context.Plans
                .Include(p => p.ActionPlan)
                    .ThenInclude(ap => ap.ProjectPhase)
                        .ThenInclude(ph => ph.Project)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (plan == null)
            {
                return NotFound();
            }
            if (!await _ministryScope.CanAccessPlansAsync(new[] { plan.Code }))
            {
                return Forbid();
            }

            return View(plan);
        }

        // POST: Plans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan != null)
            {
                if (!await _ministryScope.CanAccessPlansAsync(new[] { plan.Code }))
                {
                    return Forbid();
                }
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
