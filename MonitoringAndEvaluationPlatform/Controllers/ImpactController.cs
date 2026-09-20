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

            // Which rows offer an Edit button. Computed here rather than in the view so the
            // "may this user edit this row?" rule lives in one place (HasLinksOutsideScope) and
            // cannot drift from what Edit itself enforces. The query above already Includes
            // Ministries, Frameworks and IndicatorLinks -> ImpactIndicator -> Project, which is
            // everything the predicate reads.
            ViewBag.EditableOutputIds = outputs
                .Where(po => isAdmin || !HasLinksOutsideScope(po, scopedMinistryCode))
                .Select(po => po.Id)
                .ToHashSet();

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

            // Drives the Edit button in the header — same rule as Index, same single source.
            ViewBag.CanEditThis = isAdmin || !HasLinksOutsideScope(projectOutput, scopedMinistryCode);

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

        // GET: /Impact/Edit/5
        //
        // Same form as Create, prefilled. Everything the create form sets is editable here:
        // name, bounds, ministries, frameworks, linked indicators and their weights.
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> Edit(int id)
        {
            var projectOutput = await LoadForEditAsync(id, tracking: false);
            if (projectOutput == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();

            // Same ministry scope check as Details/Delete.
            if (!isAdmin &&
                (scopedMinistryCode is null ||
                 !projectOutput.Ministries.Any(m => m.Code == scopedMinistryCode)))
            {
                return Forbid();
            }

            // Not Forbid(): they may legitimately READ this row, so a 403 would misdescribe the
            // situation. See HasLinksOutsideScope for why a partially visible output cannot be
            // edited coherently.
            if (!isAdmin && HasLinksOutsideScope(projectOutput, scopedMinistryCode))
            {
                TempData["ErrorMessage"] = _localizer[
                    "This project output is linked to ministries, strategic goals or indicators outside your scope, so only a system administrator can edit it."].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new ProjectOutputFormViewModel
            {
                Id = projectOutput.Id,
                Name = projectOutput.Name,
                BaseValue = projectOutput.BaseValue,
                TargetValue = projectOutput.TargetValue,
                SelectedMinistryCodes = projectOutput.Ministries.Select(m => m.Code).ToList(),
                SelectedFrameworkCodes = projectOutput.Frameworks.Select(f => f.Code).ToList(),
                SelectedImpactIndicatorIds = projectOutput.IndicatorLinks.Select(l => l.ImpactIndicatorId).ToList(),
                // Doubles as the shared form's JS seed map — without it renderWeights() repaints
                // every saved weight as an equal split on first load. See _ProjectOutputForm.cshtml.
                IndicatorWeights = projectOutput.IndicatorLinks
                    .Select(l => new IndicatorWeightInput
                    {
                        ImpactIndicatorId = l.ImpactIndicatorId,
                        Weight = l.Weight
                    })
                    .ToList()
            };

            // MUST run after the Selected* lists are filled — it reads them to tick the pickers.
            await PopulatePickersAsync(model);
            return View(model);
        }

        // POST: /Impact/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> Edit([FromRoute] int id, ProjectOutputFormViewModel form)
        {
            // [FromRoute] is load-bearing, not decoration. Without it the form value provider
            // outranks the route one, so a posted Id of 6 would bind to BOTH id and form.Id, the
            // check below would pass, and a form served for /Impact/Edit/5 would quietly edit 6
            // instead. Verified: it returned 302 having edited the other row. Pinning id to the
            // route makes the mismatch detectable, which is the whole point of the check.
            if (id != form.Id) return NotFound();

            var projectOutput = await LoadForEditAsync(id, tracking: true);
            if (projectOutput == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();

            if (!isAdmin &&
                (scopedMinistryCode is null ||
                 !projectOutput.Ministries.Any(m => m.Code == scopedMinistryCode)))
            {
                return Forbid();
            }

            // Re-checked on POST, not just on GET: scope membership can move between the two (an
            // indicator's project reassigned to another ministry), and the reconciliation below
            // must never run against a graph the user cannot fully see.
            if (!isAdmin && HasLinksOutsideScope(projectOutput, scopedMinistryCode))
            {
                TempData["ErrorMessage"] = _localizer[
                    "This project output is linked to ministries, strategic goals or indicators outside your scope, so only a system administrator can edit it."].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            var selectedMinistryCodes = form.SelectedMinistryCodes ?? new List<int>();
            var selectedFrameworkCodes = form.SelectedFrameworkCodes ?? new List<int>();
            var selectedIndicatorIds = form.SelectedImpactIndicatorIds ?? new List<int>();

            // Resolve the FINAL, scope-filtered sets up front, exactly as Create does, so the
            // weight-sum check below validates precisely what will be persisted.
            //
            // Ministries load through _context.Ministries (TRACKED) rather than
            // ScopedMinistriesAsync, whose AsNoTracking() results cannot be attached to the
            // tracked graph without EF trying to INSERT a duplicate Ministry. Create takes the
            // same route for the same reason.
            var allowedMinistryCodes = (await ScopedMinistriesAsync())
                .Select(m => m.Code)
                .ToHashSet();

            var postedMinistries = selectedMinistryCodes.Any()
                ? (await _context.Ministries
                        .Where(m => selectedMinistryCodes.Contains(m.Code))
                        .ToListAsync())
                    .Where(m => allowedMinistryCodes.Contains(m.Code))
                    .ToList()
                : new List<Ministry>();

            // These two are already tracked queries, so their results are the same instances
            // LoadForEditAsync tracked — no duplicate-key risk.
            var postedFrameworks = selectedFrameworkCodes.Any()
                ? await (await ScopedFrameworksQueryAsync())
                    .Where(f => selectedFrameworkCodes.Contains(f.Code))
                    .ToListAsync()
                : new List<Framework>();

            var postedIndicators = selectedIndicatorIds.Any()
                ? await (await ScopedIndicatorsQueryAsync())
                    .Where(i => selectedIndicatorIds.Contains(i.Id))
                    .ToListAsync()
                : new List<ImpactIndicator>();

            // A non-admin who drops their own ministry loses the row for good: Index, Details,
            // Edit, SaveActualImpact and Delete all filter on it and there is no way back without
            // an administrator. Blocked rather than warned — the action has no user-side undo, and
            // the success redirect would 403 on arrival.
            if (!isAdmin && !postedMinistries.Any(m => m.Code == scopedMinistryCode))
            {
                ModelState.AddModelError(nameof(form.SelectedMinistryCodes), _localizer[
                    "You cannot remove your own ministry from this project output — you would lose access to it."].Value);
            }

            // Same rule and 0.01 tolerance as Create. Thanks to the out-of-scope guard above, the
            // set checked here IS the full persisted set — there is never a hidden link
            // contributing weight the user cannot see.
            if (postedIndicators.Any())
            {
                var totalWeight = postedIndicators
                    .Select(i => form.IndicatorWeights?.FirstOrDefault(w => w.ImpactIndicatorId == i.Id)?.Weight ?? 0)
                    .Sum();

                if (Math.Abs(totalWeight - 100) > 0.01)
                {
                    ModelState.AddModelError(string.Empty, _localizer[
                        "Indicator weights must sum to exactly 100. Current sum: {0}.",
                        totalWeight.ToString("N2")].Value);
                }
            }

            // Nothing above this line mutates the tracked entity, so a rejected post leaves the
            // row exactly as it was.
            if (!ModelState.IsValid)
            {
                await PopulatePickersAsync(form);
                return View(form);
            }

            projectOutput.Name = form.Name.Trim();
            projectOutput.BaseValue = form.BaseValue;
            projectOutput.TargetValue = form.TargetValue;
            // CreatedAt deliberately untouched — it records creation, not last edit.

            SyncScopedLinks(projectOutput.Ministries, postedMinistries,
                m => m.Code,
                m => isAdmin || m.Code == scopedMinistryCode);

            SyncScopedLinks(projectOutput.Frameworks, postedFrameworks,
                f => f.Code,
                f => isAdmin || f.MinistryCode == scopedMinistryCode);

            SyncIndicatorLinks(projectOutput, postedIndicators, form, isAdmin, scopedMinistryCode);

            await _context.SaveChangesAsync();

            // Nothing to recalculate: every impact figure is computed live off the links
            // (ProjectOutput.cs), so no *Performance column and no IPerformanceService call.

            TempData["SuccessMessage"] = _localizer["Project output updated successfully."].Value;
            return RedirectToAction(nameof(Details), new { id = projectOutput.Id });
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

        // ──────────────────────────── Edit helpers ─────────────────────────────

        /// <summary>
        /// The Edit graph. Ministries/Frameworks for the pickers and the scope checks;
        /// IndicatorLinks -> ImpactIndicator -> Project because BOTH the link reconciliation and
        /// the out-of-scope guard read Project.MinistryCode to decide whether a link is visible.
        /// Dropping the Project hop would make every existing link look out of scope and bounce
        /// every non-admin off the page — it fails quietly, so keep it.
        ///
        /// Deliberately omits YearlyValues and ActualImpacts: the Edit form renders no rolled-up
        /// figure, so loading them would only cost rows.
        /// </summary>
        private Task<ProjectOutput?> LoadForEditAsync(int id, bool tracking)
        {
            IQueryable<ProjectOutput> query = _context.ProjectOutputs
                .Include(po => po.Ministries)
                .Include(po => po.Frameworks)
                .Include(po => po.IndicatorLinks).ThenInclude(l => l.ImpactIndicator).ThenInclude(i => i.Project);

            if (!tracking) query = query.AsNoTracking();

            return query.FirstOrDefaultAsync(po => po.Id == id);
        }

        /// <summary>
        /// True when this output reaches any ministry, framework or indicator the given scope
        /// cannot see. Each clause is the in-memory twin of the matching Scoped*QueryAsync
        /// predicate.
        ///
        /// Such an output is NOT editable by a non-admin. Preserving the invisible links while
        /// reconciling the visible ones would keep the data safe, but it leaves the weight rule
        /// incoherent: the form can only show part of the set, so "weights must sum to 100"
        /// becomes either unsatisfiable (validate the whole set, show 60% of it) or a silent
        /// corruption of the invariant (validate only what is shown, persist a total that is not
        /// 100). Handing the row to an administrator, who sees all of it, is the only honest
        /// option.
        ///
        /// Only ever called with isAdmin == false. A non-admin past the ministry scope check
        /// always has a non-null scopedMinistryCode, so the null comparisons below simply read as
        /// "not visible", which is correct.
        /// </summary>
        private static bool HasLinksOutsideScope(ProjectOutput po, int? scopedMinistryCode) =>
            po.Ministries.Any(m => m.Code != scopedMinistryCode)
            || po.Frameworks.Any(f => f.MinistryCode != scopedMinistryCode)
            || po.IndicatorLinks.Any(l => l.ImpactIndicator.Project == null
                                          || l.ImpactIndicator.Project.MinistryCode != scopedMinistryCode);

        /// <summary>
        /// Reconciles a plain many-to-many collection against the posted selection, touching only
        /// the rows the editor can SEE. An existing link outside their scope was never rendered on
        /// the form, so its absence from the post means "not shown to me", not "unlink it" —
        /// removing it would be silent data loss. Additions are already scope-filtered by the
        /// caller.
        ///
        /// With the HasLinksOutsideScope guard in place this is equivalent to a full diff for
        /// every user today; it exists so the delete path stays safe if that guard is ever
        /// relaxed, and during the window where scope membership shifts between GET and POST.
        /// </summary>
        private static void SyncScopedLinks<T>(
            ICollection<T> current,
            IReadOnlyCollection<T> posted,
            Func<T, int> keyOf,
            Func<T, bool> isVisible)
        {
            var postedKeys = posted.Select(keyOf).ToHashSet();

            foreach (var dropped in current.Where(isVisible)
                                           .Where(x => !postedKeys.Contains(keyOf(x)))
                                           .ToList())      // ToList: we mutate `current` below
            {
                current.Remove(dropped);
            }

            var currentKeys = current.Select(keyOf).ToHashSet();
            foreach (var added in posted.Where(p => !currentKeys.Contains(keyOf(p))))
            {
                current.Add(added);
            }
        }

        /// <summary>
        /// Reconciles the weighted join rows: remove dropped, update Weight on survivors, add new.
        ///
        /// Diffed rather than cleared-and-re-added. Two reasons, both real:
        ///  - ProjectOutputImpactIndicator has a UNIQUE index on
        ///    (ProjectOutputId, ImpactIndicatorId). Clear-and-re-add puts the DELETE and the
        ///    re-INSERT of the same pair in one SaveChanges batch, and EF does not guarantee the
        ///    DELETE is ordered first — that is an intermittent unique-index violation, the worst
        ///    kind.
        ///  - A survivor keeps its own Id and row, so only the Weight column is written.
        ///
        /// Same visibility rule as SyncScopedLinks: a link whose indicator's project sits outside
        /// the editor's scope is left untouched.
        /// </summary>
        private void SyncIndicatorLinks(
            ProjectOutput projectOutput,
            IReadOnlyCollection<ImpactIndicator> postedIndicators,
            ProjectOutputFormViewModel form,
            bool isAdmin,
            int? scopedMinistryCode)
        {
            var postedIds = postedIndicators.Select(i => i.Id).ToHashSet();

            // 1. Remove links the user dropped — only ones the form actually offered them.
            var dropped = projectOutput.IndicatorLinks
                .Where(l => isAdmin
                            || (l.ImpactIndicator.Project != null
                                && l.ImpactIndicator.Project.MinistryCode == scopedMinistryCode))
                .Where(l => !postedIds.Contains(l.ImpactIndicatorId))
                .ToList();

            if (dropped.Any())
            {
                // Explicit Remove rather than relying on orphan-cascade, and also detached from
                // the in-memory collection so the graph stays truthful for anything reading it
                // after this point.
                _context.RemoveRange(dropped);
                foreach (var link in dropped) projectOutput.IndicatorLinks.Remove(link);
            }

            // 2. Update survivors in place, 3. add the genuinely new ones.
            foreach (var indicator in postedIndicators)
            {
                var weight = form.IndicatorWeights?
                    .FirstOrDefault(w => w.ImpactIndicatorId == indicator.Id)?.Weight ?? 0;

                var existing = projectOutput.IndicatorLinks
                    .FirstOrDefault(l => l.ImpactIndicatorId == indicator.Id);

                if (existing != null)
                {
                    existing.Weight = weight;
                }
                else
                {
                    projectOutput.IndicatorLinks.Add(new ProjectOutputImpactIndicator
                    {
                        ImpactIndicator = indicator,
                        Weight = weight
                    });
                }
            }
        }
    }
}
