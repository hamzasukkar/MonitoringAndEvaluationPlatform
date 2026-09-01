using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// The top-level Impact page: project outputs, each grouping impact indicators drawn from
    /// across projects, with their values rolled up per year.
    ///
    /// Nothing here writes a *Performance column — this is the parallel impact track, not the
    /// results-framework roll-up.
    ///
    /// Reuses the Strategy permission constants (as FrameworkGoalsController does, since this page
    /// sits beside it in the nav) rather than introducing new ones: PermissionAuthorizationHandler
    /// and RolePermissionService both switch over the constants by hand, so a new constant missing
    /// from those switches would deny everyone except SystemAdministrator.
    /// </summary>
    [Authorize]
    public class ImpactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<ImpactController> _localizer;

        public ImpactController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<ImpactController> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        // ─────────────────────────── Ministry scoping ───────────────────────────
        // Copied from FrameworkGoalsController: fails closed — a non-admin with no MinistryCode
        // gets .Where(_ => false) rather than the full list.

        private async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            if (User.IsInRole(UserRoles.SystemAdministrator))
            {
                return (true, null);
            }

            var user = await _userManager.GetUserAsync(User);
            return (false, user?.MinistryCode);
        }

        private static bool IsArabic =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        private static string MinistryName(Ministry m) =>
            IsArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN;

        // ─────────────────────────────── Actions ────────────────────────────────

        // GET: /Impact
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Index()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();

            IQueryable<ProjectOutput> query = _context.ProjectOutputs
                .Include(po => po.Ministries)
                .Include(po => po.Frameworks)
                // Both ThenIncludes are required: YearlyValues for the numbers, Project for the
                // year range. Missing either renders an empty row rather than erroring. The extra
                // IndicatorLinks hop is needed because ImpactIndicators is now a computed
                // projection over the weighted join entity, not a directly mapped collection.
                .Include(po => po.IndicatorLinks).ThenInclude(l => l.ImpactIndicator).ThenInclude(i => i.YearlyValues)
                .Include(po => po.IndicatorLinks).ThenInclude(l => l.ImpactIndicator).ThenInclude(i => i.Project)
                // Without this every Actual Impact cell silently reads as unrecorded.
                .Include(po => po.ActualImpacts);

            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(po => po.Ministries.Any(m => m.Code == scopedMinistryCode));
            }

            var outputs = await query
                .AsNoTracking()
                .OrderByDescending(po => po.Id)
                .ToListAsync();

            ViewBag.Ministries = await ScopedMinistriesAsync();
            return View(outputs);
        }

        // GET: /Impact/Details/5
        //
        // Shows which impact indicators a project output actually groups, and how each one
        // contributes to the per-year totals shown on the Index table.
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Details(int id)
        {
            var projectOutput = await _context.ProjectOutputs
                .Include(po => po.Ministries)
                .Include(po => po.Frameworks)
                // Same two ThenIncludes as Index, plus the IndicatorLinks hop for the same reason.
                .Include(po => po.IndicatorLinks).ThenInclude(l => l.ImpactIndicator).ThenInclude(i => i.YearlyValues)
                .Include(po => po.IndicatorLinks).ThenInclude(l => l.ImpactIndicator).ThenInclude(i => i.Project)
                .Include(po => po.ActualImpacts)
                .AsNoTracking()
                .FirstOrDefaultAsync(po => po.Id == id);

            if (projectOutput == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin &&
                (scopedMinistryCode is null ||
                 !projectOutput.Ministries.Any(m => m.Code == scopedMinistryCode)))
            {
                return Forbid();
            }

            return View(projectOutput);
        }

        // GET: /Impact/Create
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> Create()
        {
            var model = new ProjectOutputFormViewModel();
            await PopulatePickersAsync(model);
            return View(model);
        }

        // POST: /Impact/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> Create(ProjectOutputFormViewModel form)
        {
            var selectedIndicatorIds = form.SelectedImpactIndicatorIds ?? new List<int>();

            // Resolve the FINAL, scope-filtered indicator set up front so the weight-sum check
            // below validates exactly what will be persisted — not the raw posted ids, some of
            // which may fall outside the user's scope and get silently dropped later, which would
            // otherwise let a sum that only looked right on the posted form through.
            var scopedIndicators = selectedIndicatorIds.Any()
                ? await (await ScopedIndicatorsQueryAsync())
                    .Where(i => selectedIndicatorIds.Contains(i.Id))
                    .ToListAsync()
                : new List<ImpactIndicator>();

            // Weights across every linked indicator must sum to exactly 100 — same convention and
            // 0.01 tolerance as ProjectPhase.Weight (ProjectPhasesController.UpdateWeights).
            // Nothing to check when no indicators are linked.
            if (scopedIndicators.Any())
            {
                var totalWeight = scopedIndicators
                    .Select(i => form.IndicatorWeights?.FirstOrDefault(w => w.ImpactIndicatorId == i.Id)?.Weight ?? 0)
                    .Sum();

                if (Math.Abs(totalWeight - 100) > 0.01)
                {
                    ModelState.AddModelError(string.Empty, _localizer[
                        "Indicator weights must sum to exactly 100. Current sum: {0}.",
                        totalWeight.ToString("N2")].Value);
                }
            }

            if (!ModelState.IsValid)
            {
                // Repopulate before redisplaying, or the three pickers render empty.
                await PopulatePickersAsync(form);
                return View(form);
            }

            var projectOutput = new ProjectOutput
            {
                Name = form.Name.Trim(),
                CreatedAt = DateTime.Now,
                BaseValue = form.BaseValue,
                TargetValue = form.TargetValue
            };

            // Load the selected entities so EF writes the join rows. Anything outside the user's
            // scope is silently dropped rather than trusted from the posted form.
            var allowedMinistryCodes = (await ScopedMinistriesAsync())
                .Select(m => m.Code)
                .ToHashSet();

            var selectedMinistryCodes = form.SelectedMinistryCodes ?? new List<int>();
            var selectedFrameworkCodes = form.SelectedFrameworkCodes ?? new List<int>();

            if (selectedMinistryCodes.Any())
            {
                var ministries = await _context.Ministries
                    .Where(m => selectedMinistryCodes.Contains(m.Code))
                    .ToListAsync();

                foreach (var m in ministries.Where(m => allowedMinistryCodes.Contains(m.Code)))
                {
                    projectOutput.Ministries.Add(m);
                }
            }

            if (selectedFrameworkCodes.Any())
            {
                var frameworks = await (await ScopedFrameworksQueryAsync())
                    .Where(f => selectedFrameworkCodes.Contains(f.Code))
                    .ToListAsync();

                foreach (var f in frameworks) projectOutput.Frameworks.Add(f);
            }

            // scopedIndicators was resolved above, before the weight-sum validation, so it is
            // reused here rather than queried a second time.
            foreach (var i in scopedIndicators)
            {
                var weight = form.IndicatorWeights?.FirstOrDefault(w => w.ImpactIndicatorId == i.Id)?.Weight ?? 0;
                projectOutput.IndicatorLinks.Add(new ProjectOutputImpactIndicator
                {
                    ImpactIndicator = i,
                    Weight = weight
                });
            }

            _context.ProjectOutputs.Add(projectOutput);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Project output created successfully."].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: /Impact/SaveActualImpact
        //
        // Records the hand-observed impact for one (project output, year). Upserts: re-saving a
        // year the user already recorded updates that row rather than inserting a second one —
        // the unique index on (ProjectOutputId, Year) would reject a duplicate anyway, so this
        // turns a would-be 500 into the edit the user actually meant.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> SaveActualImpact(int projectOutputId, int year, double value)
        {
            var projectOutput = await _context.ProjectOutputs
                .Include(po => po.Ministries)
                .Include(po => po.ActualImpacts)
                // CoveredYears walks the linked indicators' projects, so both hops are needed for
                // the year check below to see anything at all.
                .Include(po => po.IndicatorLinks).ThenInclude(l => l.ImpactIndicator).ThenInclude(i => i.Project)
                .FirstOrDefaultAsync(po => po.Id == projectOutputId);

            if (projectOutput == null) return NotFound();

            // Same ministry scope check as Delete.
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin &&
                (scopedMinistryCode is null ||
                 !projectOutput.Ministries.Any(m => m.Code == scopedMinistryCode)))
            {
                return Forbid();
            }

            // Stale-form guard: only a year this output actually covers can be recorded.
            if (!projectOutput.CoveredYears.Contains(year))
            {
                TempData["ErrorMessage"] = _localizer[
                    "{0} is not one of this project output's years.", year].Value;
                return RedirectToAction(nameof(Index));
            }

            var existing = projectOutput.ActualImpacts.FirstOrDefault(a => a.Year == year);
            if (existing != null)
            {
                existing.Value = value;
                existing.DateRecorded = DateTime.Now;
            }
            else
            {
                _context.ProjectOutputActualImpacts.Add(new ProjectOutputActualImpact
                {
                    ProjectOutputId = projectOutputId,
                    Year = year,
                    Value = value,
                    DateRecorded = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Actual impact saved."].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: /Impact/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.DeleteStrategy)]
        public async Task<IActionResult> Delete(int id)
        {
            var projectOutput = await _context.ProjectOutputs
                .Include(po => po.Ministries)
                .FirstOrDefaultAsync(po => po.Id == id);

            if (projectOutput == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin &&
                (scopedMinistryCode is null ||
                 !projectOutput.Ministries.Any(m => m.Code == scopedMinistryCode)))
            {
                return Forbid();
            }

            // Only the grouping goes; the impact indicators themselves are untouched.
            _context.ProjectOutputs.Remove(projectOutput);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Project output deleted."].Value;
            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────── Cascade endpoints ──────────────────────────

        // GET: /Impact/GetFrameworks?ministryCodes=1&ministryCodes=2
        [HttpGet]
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> GetFrameworks([FromQuery] int[] ministryCodes)
        {
            var query = await ScopedFrameworksQueryAsync();

            if (ministryCodes is { Length: > 0 })
            {
                query = query.Where(f => f.MinistryCode != null
                                         && ministryCodes.Contains(f.MinistryCode.Value));
            }

            var frameworks = await query
                .OrderBy(f => f.Name)
                .Select(f => new { value = f.Code, text = f.Name })
                .ToListAsync();

            return Json(frameworks);
        }

        // GET: /Impact/GetImpactIndicators?ministryCodes=1&ministryCodes=2
        //
        // Filters by the indicator's PROJECT ministry. Framework is deliberately not a filter
        // here: ImpactIndicator has no framework FK, and the only path
        // (Framework -> Outcome -> Output -> SubOutput -> Indicator -> Project -> ImpactIndicator)
        // holds only for projects wired into the results framework.
        [HttpGet]
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> GetImpactIndicators([FromQuery] int[] ministryCodes)
        {
            var query = await ScopedIndicatorsQueryAsync();

            if (ministryCodes is { Length: > 0 })
            {
                query = query.Where(i => i.Project.MinistryCode != null
                                         && ministryCodes.Contains(i.Project.MinistryCode.Value));
            }

            var indicators = await query
                .OrderBy(i => i.Name)
                .Select(i => new { value = i.Id, text = i.Name + " — " + i.Project.ProjectName })
                .ToListAsync();

            return Json(indicators);
        }

        // ─────────────────────────────── Helpers ────────────────────────────────

        private async Task<List<Ministry>> ScopedMinistriesAsync()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            IQueryable<Ministry> query = _context.Ministries;

            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(m => m.Code == scopedMinistryCode);
            }

            return await query.AsNoTracking().ToListAsync();
        }

        private async Task<IQueryable<Framework>> ScopedFrameworksQueryAsync()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            IQueryable<Framework> query = _context.Frameworks;

            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(f => f.MinistryCode == scopedMinistryCode);
            }

            return query;
        }

        private async Task<IQueryable<ImpactIndicator>> ScopedIndicatorsQueryAsync()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            IQueryable<ImpactIndicator> query = _context.ImpactIndicators.Include(i => i.Project);

            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(i => i.Project.MinistryCode == scopedMinistryCode);
            }

            return query;
        }

        /// <summary>
        /// Fills the three picker lists. With no ministry chosen the framework and indicator lists
        /// show everything in the user's scope; the client narrows them via the cascade endpoints.
        /// </summary>
        private async Task PopulatePickersAsync(ProjectOutputFormViewModel model)
        {
            var ministries = await ScopedMinistriesAsync();
            model.AvailableMinistries = ministries
                .Select(m => new SelectListItem
                {
                    Value = m.Code.ToString(),
                    Text = MinistryName(m),
                    Selected = model.SelectedMinistryCodes?.Contains(m.Code) == true
                })
                .OrderBy(x => x.Text)
                .ToList();

            model.AvailableFrameworks = await (await ScopedFrameworksQueryAsync())
                .OrderBy(f => f.Name)
                .Select(f => new SelectListItem
                {
                    Value = f.Code.ToString(),
                    Text = f.Name
                })
                .ToListAsync();

            model.AvailableImpactIndicators = await (await ScopedIndicatorsQueryAsync())
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name + " — " + i.Project.ProjectName
                })
                .ToListAsync();

            // Re-apply selections after materialising (Selected cannot be set inside the
            // EF projection above without translating the Contains into SQL).
            foreach (var f in model.AvailableFrameworks)
            {
                f.Selected = model.SelectedFrameworkCodes?.Contains(int.Parse(f.Value)) == true;
            }
            foreach (var i in model.AvailableImpactIndicators)
            {
                i.Selected = model.SelectedImpactIndicatorIds?.Contains(int.Parse(i.Value)) == true;
            }
        }
    }
}
