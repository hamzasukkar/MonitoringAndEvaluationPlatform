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
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    // Login required: nothing here is public. No fallback policy exists, so without this
    // attribute every action was reachable anonymously.
    //
    // Every action checks that the action plan / phase belongs to one of the caller's own
    // projects. Which ROLES may edit plans is a separate, still-open decision, so no
    // role-restricting [Permission] attribute is applied here.
    [Authorize]
    public class ActionPlansController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMinistryScopeService _ministryScope;

        public ActionPlansController(ApplicationDbContext context, IMinistryScopeService ministryScope)
        {
            _context = context;
            _ministryScope = ministryScope;
        }

        private async Task<SelectList> PhaseOptionsAsync(int? selected = null) =>
            new SelectList(
                await _context.ProjectPhases.WithinScope(await _ministryScope.GetScopeAsync()).Include(pp => pp.Project).ToListAsync(),
                "Id", "Name", selected);

        // GET: ActionPlans
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ActionPlans
                .WithinScope(await _ministryScope.GetScopeAsync())
                .Include(a => a.ProjectPhase)
                    .ThenInclude(pp => pp.Project);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> Test()
        {
            return View();
        }

        // GET: ActionPlans/ActionPlan?phaseId=5
        public async Task<IActionResult> ActionPlan(int phaseId)
        {
            // Checked before anything else: a missing action plan is auto-created below, so an
            // unchecked foreign phaseId would also be a write into another ministry's project.
            if (await _context.ProjectPhases.AnyAsync(p => p.Id == phaseId)
                && !await _ministryScope.CanAccessPhaseAsync(phaseId))
            {
                return Forbid();
            }

            // Fetch action plan for this specific phase
            var phaseActionPlan = await _context.ActionPlans
                .Include(ap => ap.Plans)
                .Include(ap => ap.ProjectPhase)
                    .ThenInclude(pp => pp.Project)
                .FirstOrDefaultAsync(ap => ap.ProjectPhaseId == phaseId);

            // Auto-create ActionPlan if it doesn't exist yet (e.g. phases created before the auto-create fix)
            if (phaseActionPlan == null)
            {
                var orphanPhase = await _context.ProjectPhases
                    .Include(p => p.Project)
                    .FirstOrDefaultAsync(p => p.Id == phaseId);

                if (orphanPhase == null) return NotFound();

                int plansCount = ((orphanPhase.EndDate.Year - orphanPhase.StartDate.Year) * 12) + orphanPhase.EndDate.Month - orphanPhase.StartDate.Month;
                if (orphanPhase.EndDate.Day < orphanPhase.StartDate.Day) plansCount--;
                if (plansCount <= 0) plansCount = 1;

                var newPlan = new ActionPlan { ProjectPhaseId = phaseId, PlansCount = plansCount };
                _context.ActionPlans.Add(newPlan);
                await _context.SaveChangesAsync();

                phaseActionPlan = await _context.ActionPlans
                    .Include(ap => ap.Plans)
                    .Include(ap => ap.ProjectPhase)
                        .ThenInclude(pp => pp.Project)
                    .FirstOrDefaultAsync(ap => ap.Code == newPlan.Code);

                if (phaseActionPlan == null) return NotFound();
            }

            var phase = phaseActionPlan.ProjectPhase;
            var project = phase.Project;

            var viewModel = phaseActionPlan.Plans.OrderBy(p => p.Date).Select(plan => new PlanDetail
            {
                PlanCode = plan.Code,
                Date = plan.Date,
                RealisedValue = plan.Realised
            }).ToList();

            ViewBag.PhaseId = phaseId;
            ViewBag.ProjectID = project.ProjectID;
            ViewBag.PhaseName = phase.Name;
            ViewBag.ActionPlanCode = phaseActionPlan.Code;
            ViewBag.PhaseBudget = phase.Budget;
            ViewBag.CurrencySymbol = project.CurrencySymbol;

            // Calculate months for display using full project dates
            var months = new List<DateTime>();
            var currentMonth = new DateTime(project.StartDate.Year, project.StartDate.Month, 1);
            var endMonth = new DateTime(project.EndDate.Year, project.EndDate.Month, 1);

            while (currentMonth <= endMonth)
            {
                months.Add(currentMonth);
                currentMonth = currentMonth.AddMonths(1);
            }

            ViewBag.ProjectMonths = months;
            ViewBag.ProjectStartDate = project.StartDate;
            ViewBag.ProjectEndDate = project.EndDate;

            return View(viewModel);
        }

        // GET: ActionPlans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var actionPlan = await _context.ActionPlans
                .Include(a => a.ProjectPhase)
                    .ThenInclude(pp => pp.Project)
                .FirstOrDefaultAsync(m => m.Code == id);

            if (actionPlan == null) return NotFound();
            if (!await _ministryScope.CanAccessActionPlanAsync(actionPlan.Code)) return Forbid();

            return View(actionPlan);
        }

        // GET: ActionPlans/Create
        public async Task<IActionResult> Create()
        {
            ViewData["ProjectPhaseId"] = await PhaseOptionsAsync();
            return View();
        }

        // POST: ActionPlans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActionPlan actionPlan)
        {
            if (!await _ministryScope.CanAccessPhaseAsync(actionPlan.ProjectPhaseId)) return Forbid();

            if (ModelState.IsValid || true)
            {
                _context.Add(actionPlan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectPhaseId"] = await PhaseOptionsAsync(actionPlan.ProjectPhaseId);
            return View(actionPlan);
        }

        // GET: ActionPlans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var actionPlan = await _context.ActionPlans.FindAsync(id);
            if (actionPlan == null) return NotFound();
            if (!await _ministryScope.CanAccessActionPlanAsync(actionPlan.Code)) return Forbid();

            ViewData["ProjectPhaseId"] = await PhaseOptionsAsync(actionPlan.ProjectPhaseId);
            return View(actionPlan);
        }

        // POST: ActionPlans/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,PlansCount,ProjectPhaseId")] ActionPlan actionPlan)
        {
            if (id != actionPlan.Code) return NotFound();

            // The form binds ProjectPhaseId, so it could also move the action plan into another
            // ministry's phase.
            if (!await _ministryScope.CanAccessActionPlanAsync(id)
                || !await _ministryScope.CanAccessPhaseAsync(actionPlan.ProjectPhaseId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(actionPlan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActionPlanExists(actionPlan.Code)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectPhaseId"] = await PhaseOptionsAsync(actionPlan.ProjectPhaseId);
            return View(actionPlan);
        }

        // GET: ActionPlans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var actionPlan = await _context.ActionPlans
                .Include(a => a.ProjectPhase)
                    .ThenInclude(pp => pp.Project)
                .FirstOrDefaultAsync(m => m.Code == id);

            if (actionPlan == null) return NotFound();
            if (!await _ministryScope.CanAccessActionPlanAsync(actionPlan.Code)) return Forbid();

            return View(actionPlan);
        }

        // POST: ActionPlans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actionPlan = await _context.ActionPlans.FindAsync(id);
            if (actionPlan != null)
            {
                if (!await _ministryScope.CanAccessActionPlanAsync(actionPlan.Code)) return Forbid();
                _context.ActionPlans.Remove(actionPlan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActionPlanExists(int id)
        {
            return _context.ActionPlans.Any(e => e.Code == id);
        }
    }
}
