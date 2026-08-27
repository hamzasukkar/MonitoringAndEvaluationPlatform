using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// Impact indicators live under a project and are measured independently of execution
    /// progress. Nothing in this controller writes a *Performance column.
    ///
    /// AchievedValue is typed in directly by the user — there is no dated achievement ledger and
    /// no recalculation service behind it. AchievementRate is computed from it at render time.
    ///
    /// Reuses the existing Indicator permissions rather than introducing new constants, because
    /// PermissionAuthorizationHandler and RolePermissionService both switch over them by hand.
    /// </summary>
    public class ImpactIndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<ImpactIndicatorsController> _localizer;

        public ImpactIndicatorsController(
            ApplicationDbContext context,
            IStringLocalizer<ImpactIndicatorsController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: ImpactIndicators?projectId=5
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Index(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.ImpactIndicators)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectID == projectId);

            if (project == null) return NotFound();

            ViewBag.Project = project;
            return View(project.ImpactIndicators.OrderBy(i => i.Name).ToList());
        }

        // GET: ImpactIndicators/Details/5
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Details(int id)
        {
            var indicator = await _context.ImpactIndicators
                .Include(i => i.Project)
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
            return View(new ImpactIndicator { ProjectID = projectId });
        }

        // POST: ImpactIndicators/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> Create(
            [Bind("Name,Unit,Description,TargetValue,AchievedValue,ProjectID")] ImpactIndicator indicator)
        {
            ModelState.Remove(nameof(indicator.Project));

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectID == indicator.ProjectID);

            if (project == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Project = project;
                return View(indicator);
            }

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
            [Bind("Id,Name,Unit,Description,TargetValue,AchievedValue,ProjectID")] ImpactIndicator indicator)
        {
            if (id != indicator.Id) return NotFound();

            ModelState.Remove(nameof(indicator.Project));

            var existing = await _context.ImpactIndicators
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Project = existing.Project;
                return View(indicator);
            }

            existing.Name = indicator.Name;
            existing.Unit = indicator.Unit;
            existing.Description = indicator.Description;
            existing.TargetValue = indicator.TargetValue;
            existing.AchievedValue = indicator.AchievedValue;

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

            _context.ImpactIndicators.Remove(indicator);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Impact indicator deleted."].Value;
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }
    }
}
