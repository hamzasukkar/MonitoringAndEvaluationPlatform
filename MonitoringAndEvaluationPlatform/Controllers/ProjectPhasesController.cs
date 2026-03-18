using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class ProjectPhasesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MonitoringService _monitoringService;
        private readonly IStringLocalizer<ProjectPhasesController> _localizer;

        public ProjectPhasesController(ApplicationDbContext context, MonitoringService monitoringService, IStringLocalizer<ProjectPhasesController> localizer)
        {
            _context = context;
            _monitoringService = monitoringService;
            _localizer = localizer;
        }

        // GET: ProjectPhases/Create?projectId=5
        public async Task<IActionResult> Create(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Phases)
                .FirstOrDefaultAsync(p => p.ProjectID == projectId);

            if (project == null) return NotFound();

            var usedBudget = project.Phases.Sum(pp => pp.Budget);
            ViewBag.Project = project;
            ViewBag.RemainingBudget = project.EstimatedBudget - usedBudget;
            ViewBag.PhaseCount = project.Phases.Count;

            var phase = new ProjectPhase
            {
                ProjectID = projectId,
                StartDate = project.StartDate,
                EndDate = project.EndDate
            };
            return View(phase);
        }

        // POST: ProjectPhases/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,StartDate,EndDate,Budget,TargetQuantity,ProjectID")] ProjectPhase phase)
        {
            ModelState.Remove(nameof(phase.Project));
            ModelState.Remove(nameof(phase.Measures));
            ModelState.Remove(nameof(phase.ActionPlan));

            var project = await _context.Projects
                .Include(p => p.Phases)
                .FirstOrDefaultAsync(p => p.ProjectID == phase.ProjectID);

            if (project == null) return NotFound();

            // Validate dates within project dates
            if (phase.StartDate < project.StartDate || phase.EndDate > project.EndDate)
            {
                ModelState.AddModelError("", $"Phase dates must be within the project dates ({project.StartDate:MMM yyyy} – {project.EndDate:MMM yyyy}).");
            }

            if (phase.StartDate > phase.EndDate)
            {
                ModelState.AddModelError("EndDate", "End date must be on or after start date.");
            }

            // Validate budget (sum must not exceed project budget)
            var existingBudgetSum = project.Phases.Sum(pp => pp.Budget);
            if (existingBudgetSum + phase.Budget > project.EstimatedBudget)
            {
                ModelState.AddModelError("Budget", $"Total phase budgets cannot exceed the project budget of {project.EstimatedBudget:N2}. Remaining: {project.EstimatedBudget - existingBudgetSum:N2}.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Project = project;
                ViewBag.RemainingBudget = project.EstimatedBudget - existingBudgetSum;
                ViewBag.PhaseCount = project.Phases.Count;
                return View(phase);
            }

            _context.ProjectPhases.Add(phase);
            await _context.SaveChangesAsync();

            // Auto-create an ActionPlan for this phase with PlansCount = months in phase
            int plansCount = ((phase.EndDate.Year - phase.StartDate.Year) * 12) + phase.EndDate.Month - phase.StartDate.Month;
            if (phase.EndDate.Day < phase.StartDate.Day) plansCount--;
            if (plansCount <= 0) plansCount = 1;

            var actionPlan = new ActionPlan
            {
                ProjectPhaseId = phase.Id,
                PlansCount = plansCount
            };
            _context.ActionPlans.Add(actionPlan);
            await _context.SaveChangesAsync();

            // Auto-create one Activity per ActivityType with monthly Plans
            await CreateActivitiesWithPlans(actionPlan.Code, phase.StartDate, phase.EndDate, phase.Budget);

            // Redistribute weights equally among all phases
            await RedistributeWeightsEqually(phase.ProjectID);

            TempData["SuccessMessage"] = _localizer["Phase created successfully. Weights have been redistributed equally."].Value;
            return RedirectToAction("Details", "Projects", new { id = phase.ProjectID, tab = "phases" });
        }

        // GET: ProjectPhases/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var phase = await _context.ProjectPhases
                .Include(pp => pp.Project)
                    .ThenInclude(p => p.Phases)
                .FirstOrDefaultAsync(pp => pp.Id == id);

            if (phase == null) return NotFound();

            ViewBag.Project = phase.Project;
            ViewBag.TotalPhases = phase.Project.Phases.Count;
            return View(phase);
        }

        // POST: ProjectPhases/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,StartDate,EndDate,Budget,Weight,TargetQuantity,ProjectID")] ProjectPhase phase)
        {
            if (id != phase.Id) return NotFound();

            ModelState.Remove(nameof(phase.Project));
            ModelState.Remove(nameof(phase.Measures));
            ModelState.Remove(nameof(phase.ActionPlan));

            var project = await _context.Projects
                .Include(p => p.Phases)
                .FirstOrDefaultAsync(p => p.ProjectID == phase.ProjectID);

            if (project == null) return NotFound();

            // Validate dates
            if (phase.StartDate < project.StartDate || phase.EndDate > project.EndDate)
            {
                ModelState.AddModelError("", $"Phase dates must be within the project dates ({project.StartDate:MMM yyyy} – {project.EndDate:MMM yyyy}).");
            }

            if (phase.StartDate > phase.EndDate)
            {
                ModelState.AddModelError("EndDate", "End date must be on or after start date.");
            }

            // Validate budget (sum of other phases + this phase must not exceed project budget)
            var otherBudgetSum = project.Phases.Where(pp => pp.Id != id).Sum(pp => pp.Budget);
            if (otherBudgetSum + phase.Budget > project.EstimatedBudget)
            {
                ModelState.AddModelError("Budget", $"Total phase budgets cannot exceed the project budget of {project.EstimatedBudget:N2}. Other phases use {otherBudgetSum:N2}, leaving {project.EstimatedBudget - otherBudgetSum:N2}.");
            }

            // Validate weight: it will be validated as part of the weight group
            if (phase.Weight < 0 || phase.Weight > 100)
            {
                ModelState.AddModelError("Weight", "Weight must be between 0 and 100.");
            }

            if (!ModelState.IsValid)
            {
                phase.Project = project;
                ViewBag.Project = project;
                ViewBag.TotalPhases = project.Phases.Count;
                return View(phase);
            }

            try
            {
                var existingPhase = await _context.ProjectPhases.FindAsync(id);
                if (existingPhase == null) return NotFound();

                existingPhase.Name = phase.Name;
                existingPhase.StartDate = phase.StartDate;
                existingPhase.EndDate = phase.EndDate;
                existingPhase.Budget = phase.Budget;
                existingPhase.Weight = phase.Weight;
                existingPhase.TargetQuantity = phase.TargetQuantity;

                _context.ProjectPhases.Update(existingPhase);
                await _context.SaveChangesAsync();

                // Recalculate project performance with updated weight
                await _monitoringService.UpdateProjectPerformanceFromPhases(phase.ProjectID);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ProjectPhases.Any(pp => pp.Id == id))
                    return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = _localizer["Phase updated successfully."].Value;
            return RedirectToAction("Details", "Projects", new { id = phase.ProjectID, tab = "phases" });
        }

        // POST: ProjectPhases/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var phase = await _context.ProjectPhases.FindAsync(id);
            if (phase == null) return NotFound();

            int projectId = phase.ProjectID;

            _context.ProjectPhases.Remove(phase);
            await _context.SaveChangesAsync();

            // Redistribute weights equally among remaining phases
            await RedistributeWeightsEqually(projectId);

            // Recalculate project performance
            await _monitoringService.UpdateProjectPerformanceFromPhases(projectId);
            await _monitoringService.UpdateDisbursementPerformancesForProject(projectId);

            TempData["SuccessMessage"] = _localizer["Phase deleted. Weights have been redistributed equally."].Value;
            return RedirectToAction("Details", "Projects", new { id = projectId, tab = "phases" });
        }

        // POST: ProjectPhases/CreateAjax  (AJAX inline form from Indicators/Index)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax([Bind("Name,StartDate,EndDate,Budget,TargetQuantity,ProjectID")] ProjectPhase phase)
        {
            ModelState.Remove(nameof(phase.Project));
            ModelState.Remove(nameof(phase.Measures));
            ModelState.Remove(nameof(phase.ActionPlan));

            var project = await _context.Projects
                .Include(p => p.Phases)
                .FirstOrDefaultAsync(p => p.ProjectID == phase.ProjectID);

            if (project == null)
                return Json(new { success = false, message = "Project not found." });

            if (phase.StartDate < project.StartDate || phase.EndDate > project.EndDate)
                return Json(new { success = false, message = $"Phase dates must be within the project dates ({project.StartDate:MMM yyyy} – {project.EndDate:MMM yyyy})." });

            if (phase.StartDate > phase.EndDate)
                return Json(new { success = false, message = "End date must be on or after start date." });

            var existingBudgetSum = project.Phases.Sum(pp => pp.Budget);
            if (existingBudgetSum + phase.Budget > project.EstimatedBudget)
                return Json(new { success = false, message = $"Total phase budgets cannot exceed the project budget of {project.EstimatedBudget:N2}. Remaining: {project.EstimatedBudget - existingBudgetSum:N2}." });

            _context.ProjectPhases.Add(phase);
            await _context.SaveChangesAsync();

            int plansCount = ((phase.EndDate.Year - phase.StartDate.Year) * 12) + phase.EndDate.Month - phase.StartDate.Month;
            if (phase.EndDate.Day < phase.StartDate.Day) plansCount--;
            if (plansCount <= 0) plansCount = 1;

            var actionPlan = new ActionPlan { ProjectPhaseId = phase.Id, PlansCount = plansCount };
            _context.ActionPlans.Add(actionPlan);
            await _context.SaveChangesAsync();

            // Auto-create one Activity per ActivityType with monthly Plans
            await CreateActivitiesWithPlans(actionPlan.Code, phase.StartDate, phase.EndDate, phase.Budget);

            await RedistributeWeightsEqually(phase.ProjectID);

            return Json(new { success = true, message = "Phase created successfully." });
        }

        // POST: ProjectPhases/UpdateWeights  (AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateWeights([FromBody] UpdateWeightsDto dto)
        {
            if (dto?.Phases == null || !dto.Phases.Any())
                return BadRequest(new { message = "No phase data provided." });

            // Validate sum == 100
            var total = dto.Phases.Sum(p => p.Weight);
            if (Math.Abs(total - 100m) > 0.01m)
                return BadRequest(new { message = $"Weights must sum to exactly 100. Current sum: {total:F2}." });

            // Validate all weights are non-negative
            if (dto.Phases.Any(p => p.Weight < 0))
                return BadRequest(new { message = "All weights must be non-negative." });

            foreach (var phaseDto in dto.Phases)
            {
                var phase = await _context.ProjectPhases.FindAsync(phaseDto.PhaseId);
                if (phase == null) continue;
                phase.Weight = phaseDto.Weight;
                _context.ProjectPhases.Update(phase);
            }

            await _context.SaveChangesAsync();

            // Recalculate project performance with new weights
            if (dto.Phases.Any())
            {
                var firstPhase = await _context.ProjectPhases.FindAsync(dto.Phases.First().PhaseId);
                if (firstPhase != null)
                    await _monitoringService.UpdateProjectPerformanceFromPhases(firstPhase.ProjectID);
            }

            return Ok(new { message = "Weights updated successfully." });
        }

        // GET: ProjectPhases/GetPhasePerformance/5 (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetPhasePerformance(int id)
        {
            var phase = await _context.ProjectPhases
                .Include(pp => pp.Measures)
                .FirstOrDefaultAsync(pp => pp.Id == id);

            if (phase == null) return NotFound();

            return Ok(new
            {
                phaseId = phase.Id,
                phaseName = phase.Name,
                performance = phase.PhasePerformance,
                weight = phase.Weight,
                measuresCount = phase.Measures.Count
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPER: Redistribute weights equally among all phases of a project
        // ─────────────────────────────────────────────────────────────────────
        private async Task RedistributeWeightsEqually(int projectId)
        {
            var phases = await _context.ProjectPhases
                .Where(pp => pp.ProjectID == projectId)
                .OrderBy(pp => pp.Id)
                .ToListAsync();

            if (!phases.Any()) return;

            int count = phases.Count;
            decimal equalWeight = Math.Round(100m / count, 2);
            decimal totalAssigned = equalWeight * count;
            decimal remainder = 100m - totalAssigned;

            for (int i = 0; i < phases.Count; i++)
            {
                phases[i].Weight = equalWeight;
                // Assign the remainder to the last phase to ensure exact sum of 100
                if (i == phases.Count - 1)
                {
                    phases[i].Weight += remainder;
                }
                _context.ProjectPhases.Update(phases[i]);
            }

            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPER: Create one Activity per ActivityType with one Plan per month
        // All Planned values start at 0 (no automatic distribution)
        // ─────────────────────────────────────────────────────────────────────
        private async Task CreateActivitiesWithPlans(int actionPlanCode, DateTime startDate, DateTime endDate, double budget)
        {
            var startMonth = new DateTime(startDate.Year, startDate.Month, 1);
            var endMonth   = new DateTime(endDate.Year,   endDate.Month,   1);

            foreach (ActivityType type in Enum.GetValues(typeof(ActivityType)))
            {
                var activity = new Activity
                {
                    Name = type.ToString(),
                    ActionPlanCode = actionPlanCode,
                    ActivityType = type
                };

                var current = startMonth;
                int i = 1;
                while (current <= endMonth)
                {
                    activity.Plans.Add(new Plan
                    {
                        Name = $"{type}-{i}",
                        Date = current,
                        Planned = 0,
                        Realised = 0,
                        Activity = activity
                    });
                    current = current.AddMonths(1);
                    i++;
                }

                _context.Activities.Add(activity);
            }

            await _context.SaveChangesAsync();
        }

        // DTOs for AJAX
        public class UpdateWeightsDto
        {
            public List<PhaseWeightItem> Phases { get; set; } = new();
        }

        public class PhaseWeightItem
        {
            public int PhaseId { get; set; }
            public decimal Weight { get; set; }
        }
    }
}
