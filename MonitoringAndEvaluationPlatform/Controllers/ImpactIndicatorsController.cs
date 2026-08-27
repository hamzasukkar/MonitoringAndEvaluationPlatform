using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// Impact indicators live under a project and are measured independently of execution
    /// progress. Nothing in this controller writes a *Performance column — see
    /// <see cref="ImpactIndicatorService"/>.
    ///
    /// Reuses the existing Indicator permissions rather than introducing new constants, because
    /// PermissionAuthorizationHandler and RolePermissionService both switch over them by hand.
    /// </summary>
    public class ImpactIndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ImpactIndicatorService _impactIndicatorService;
        private readonly IStringLocalizer<ImpactIndicatorsController> _localizer;

        public ImpactIndicatorsController(
            ApplicationDbContext context,
            ImpactIndicatorService impactIndicatorService,
            IStringLocalizer<ImpactIndicatorsController> localizer)
        {
            _context = context;
            _impactIndicatorService = impactIndicatorService;
            _localizer = localizer;
        }

        // GET: ImpactIndicators?projectId=5
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Index(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.ImpactIndicators)
                    .ThenInclude(i => i.Achievements)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectID == projectId);

            if (project == null) return NotFound();

            ViewBag.Project = project;
            return View(project.ImpactIndicators.OrderBy(i => i.StartDate).ToList());
        }

        // GET: ImpactIndicators/Details/5
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Details(int id)
        {
            var indicator = await _context.ImpactIndicators
                .Include(i => i.Project)
                .Include(i => i.Achievements)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (indicator == null) return NotFound();

            return View(indicator);
        }

        // GET: ImpactIndicators/Create?projectId=5
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> Create(int projectId)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectID == projectId);

            if (project == null) return NotFound();

            ViewBag.Project = project;

            // Default the indicator to the project's own window — the common case.
            return View(new ImpactIndicator
            {
                ProjectID = projectId,
                StartDate = project.StartDate,
                EndDate = project.EndDate
            });
        }

        // POST: ImpactIndicators/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> Create(
            [Bind("Name,Unit,Description,TargetValue,StartDate,EndDate,ProjectID")] ImpactIndicator indicator)
        {
            ModelState.Remove(nameof(indicator.Project));
            ModelState.Remove(nameof(indicator.Achievements));

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectID == indicator.ProjectID);

            if (project == null) return NotFound();

            ValidateIndicatorDates(indicator, project);

            if (!ModelState.IsValid)
            {
                ViewBag.Project = project;
                return View(indicator);
            }

            indicator.AchievedValue = 0;
            _context.ImpactIndicators.Add(indicator);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Impact indicator created successfully."].Value;
            return RedirectToAction(nameof(Details), new { id = indicator.Id });
        }

        // GET: ImpactIndicators/Edit/5
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> Edit(int id)
        {
            var indicator = await _context.ImpactIndicators
                .Include(i => i.Project)
                .Include(i => i.Achievements)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (indicator == null) return NotFound();

            ViewBag.Project = indicator.Project;
            return View(indicator);
        }

        // POST: ImpactIndicators/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Unit,Description,TargetValue,StartDate,EndDate,ProjectID")] ImpactIndicator indicator)
        {
            if (id != indicator.Id) return NotFound();

            ModelState.Remove(nameof(indicator.Project));
            ModelState.Remove(nameof(indicator.Achievements));

            var existing = await _context.ImpactIndicators
                .Include(i => i.Project)
                .Include(i => i.Achievements)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (existing == null) return NotFound();

            var project = existing.Project;
            ValidateIndicatorDates(indicator, project);

            // Narrowing the window must not strand achievements outside it.
            var stranded = existing.Achievements
                .Where(a => a.Date < indicator.StartDate || a.Date > indicator.EndDate)
                .ToList();

            if (stranded.Any())
            {
                var earliest = existing.Achievements.Min(a => a.Date);
                var latest = existing.Achievements.Max(a => a.Date);
                ModelState.AddModelError("", _localizer[
                    "{0} recorded achievement(s) fall outside the new period. Existing achievements run from {1} to {2}.",
                    stranded.Count, earliest.ToString("dd/MM/yyyy"), latest.ToString("dd/MM/yyyy")].Value);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Project = project;
                indicator.AchievedValue = existing.AchievedValue;
                indicator.Achievements = existing.Achievements;
                return View(indicator);
            }

            existing.Name = indicator.Name;
            existing.Unit = indicator.Unit;
            existing.Description = indicator.Description;
            existing.TargetValue = indicator.TargetValue;
            existing.StartDate = indicator.StartDate;
            existing.EndDate = indicator.EndDate;

            // AchievedValue is untouched: editing the target changes the rate, which is computed.
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Impact indicator updated successfully."].Value;
            return RedirectToAction(nameof(Details), new { id = existing.Id });
        }

        // POST: ImpactIndicators/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.DeleteIndicator)]
        public async Task<IActionResult> Delete(int id)
        {
            var indicator = await _context.ImpactIndicators.FindAsync(id);
            if (indicator == null) return NotFound();

            int projectId = indicator.ProjectID;

            // Framework impact targets hold this indicator with DeleteBehavior.Restrict, so the
            // delete would fail at the database with a raw 500. Check first and say which targets
            // depend on it — the user can then unlink it there and retry.
            var dependentTargets = await _context.FrameworkImpactIndicators
                .Where(l => l.ImpactIndicatorId == id)
                .Select(l => l.FrameworkImpact.Name)
                .ToListAsync();

            if (dependentTargets.Any())
            {
                TempData["ErrorMessage"] = _localizer[
                    "This indicator cannot be deleted because {0} impact target(s) depend on it: {1}. Unlink it there first.",
                    dependentTargets.Count, string.Join("، ", dependentTargets)].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            // Achievements go with it via the cascade configured in ApplicationDbContext.
            _context.ImpactIndicators.Remove(indicator);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Impact indicator deleted along with its achievements."].Value;
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Achievements
        // ─────────────────────────────────────────────────────────────────────

        // POST: ImpactIndicators/AddAchievement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> AddAchievement(
            [Bind("Date,Value,Note,ImpactIndicatorId")] ImpactAchievement achievement)
        {
            ModelState.Remove(nameof(achievement.ImpactIndicator));

            var indicator = await _context.ImpactIndicators
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == achievement.ImpactIndicatorId);

            if (indicator == null) return NotFound();

            ValidateAchievementDate(achievement, indicator);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = FlattenModelStateErrors();
                return RedirectToAction(nameof(Details), new { id = achievement.ImpactIndicatorId });
            }

            _context.ImpactAchievements.Add(achievement);
            await _context.SaveChangesAsync();

            await _impactIndicatorService.RecalculateAsync(achievement.ImpactIndicatorId);

            TempData["SuccessMessage"] = _localizer["Achievement recorded."].Value;
            return RedirectToAction(nameof(Details), new { id = achievement.ImpactIndicatorId });
        }

        // POST: ImpactIndicators/EditAchievement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> EditAchievement(
            [Bind("Id,Date,Value,Note,ImpactIndicatorId")] ImpactAchievement achievement)
        {
            ModelState.Remove(nameof(achievement.ImpactIndicator));

            var existing = await _context.ImpactAchievements
                .Include(a => a.ImpactIndicator)
                .FirstOrDefaultAsync(a => a.Id == achievement.Id);

            if (existing == null) return NotFound();

            var indicator = existing.ImpactIndicator;
            ValidateAchievementDate(achievement, indicator);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = FlattenModelStateErrors();
                return RedirectToAction(nameof(Details), new { id = indicator.Id });
            }

            existing.Date = achievement.Date;
            existing.Value = achievement.Value;
            existing.Note = achievement.Note;
            await _context.SaveChangesAsync();

            await _impactIndicatorService.RecalculateAsync(indicator.Id);

            TempData["SuccessMessage"] = _localizer["Achievement updated."].Value;
            return RedirectToAction(nameof(Details), new { id = indicator.Id });
        }

        // POST: ImpactIndicators/DeleteAchievement/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.DeleteIndicator)]
        public async Task<IActionResult> DeleteAchievement(int id)
        {
            var achievement = await _context.ImpactAchievements.FindAsync(id);
            if (achievement == null) return NotFound();

            int indicatorId = achievement.ImpactIndicatorId;

            _context.ImpactAchievements.Remove(achievement);
            await _context.SaveChangesAsync();

            await _impactIndicatorService.RecalculateAsync(indicatorId);

            TempData["SuccessMessage"] = _localizer["Achievement deleted."].Value;
            return RedirectToAction(nameof(Details), new { id = indicatorId });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Validation helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// An indicator's window must sit inside the project's — the same rule ProjectPhases
        /// applies to phase dates.
        /// </summary>
        private void ValidateIndicatorDates(ImpactIndicator indicator, Project project)
        {
            if (indicator.StartDate < project.StartDate || indicator.EndDate > project.EndDate)
            {
                ModelState.AddModelError("", _localizer[
                    "Indicator dates must be within the project dates ({0} – {1}).",
                    project.StartDate.ToString("dd/MM/yyyy"),
                    project.EndDate.ToString("dd/MM/yyyy")].Value);
            }

            if (indicator.StartDate > indicator.EndDate)
            {
                ModelState.AddModelError(nameof(indicator.EndDate),
                    _localizer["End date must be on or after start date."].Value);
            }
        }

        /// <summary>
        /// Achievements must fall inside their indicator's window. Note there is deliberately no
        /// cap on the running total — exceeding the target is real information, not an error.
        /// </summary>
        private void ValidateAchievementDate(ImpactAchievement achievement, ImpactIndicator indicator)
        {
            if (achievement.Date < indicator.StartDate || achievement.Date > indicator.EndDate)
            {
                ModelState.AddModelError(nameof(achievement.Date), _localizer[
                    "Achievement date must be within the indicator period ({0} – {1}).",
                    indicator.StartDate.ToString("dd/MM/yyyy"),
                    indicator.EndDate.ToString("dd/MM/yyyy")].Value);
            }
        }

        /// <summary>
        /// Achievements are posted from the indicator's Details page, so errors have to survive a
        /// redirect rather than being re-rendered into a form.
        /// </summary>
        private string FlattenModelStateErrors()
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();

            return errors.Any()
                ? string.Join(" ", errors)
                : _localizer["Please check the values you entered."].Value;
        }
    }
}
