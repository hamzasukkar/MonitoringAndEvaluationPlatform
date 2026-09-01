using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// Impact indicators live under a project and are measured independently of execution
    /// progress. Nothing in this controller writes a *Performance column.
    ///
    /// Achievement is cumulative: the per-year values sum to the achieved total, which is compared
    /// against the indicator's target. The set of editable years is derived from the project's
    /// StartDate/EndDate on every request (Project.CoveredYears) and is never stored.
    ///
    /// Reuses the existing Indicator permissions rather than introducing new constants, because
    /// PermissionAuthorizationHandler and RolePermissionService both switch over them by hand and
    /// a constant missing from those switches denies everyone except SystemAdministrator.
    ///
    /// Scoping follows ProjectsController (per-controller GetScopeAsync, fails closed) rather than
    /// MeasuresController/ActionPlansController, which have no authorization at all.
    /// </summary>
    [Authorize]
    public class ImpactIndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<ImpactIndicatorsController> _localizer;

        public ImpactIndicatorsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<ImpactIndicatorsController> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        // ─────────────────────────── Ministry scoping ───────────────────────────
        // Copied from ProjectsController: no MinistryCode ⇒ access to nothing (fails closed).

        private async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            if (User.IsInRole(UserRoles.SystemAdministrator))
            {
                return (true, null);
            }

            var user = await _userManager.GetUserAsync(User);
            return (false, user?.MinistryCode);
        }

        private async Task<bool> ProjectBelongsToScopeAsync(int projectId)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (isAdmin) return true;
            if (scopedMinistryCode is null) return false;

            return await _context.Projects
                .Where(p => p.ProjectID == projectId)
                .AnyAsync(p => p.MinistryCode == scopedMinistryCode);
        }

        // ─────────────────────────────── Actions ────────────────────────────────

        // GET: ImpactIndicators?projectId=5
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Index(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.ImpactIndicators)
                    .ThenInclude(ii => ii.YearlyValues)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectID == projectId);

            if (project == null) return NotFound();
            if (!await ProjectBelongsToScopeAsync(projectId)) return Forbid();

            ViewBag.Project = project;
            return View(project.ImpactIndicators.OrderBy(i => i.Name).ToList());
        }

        // GET: ImpactIndicators/Details/5
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Details(int id)
        {
            var indicator = await _context.ImpactIndicators
                .Include(i => i.Project)
                .Include(i => i.YearlyValues)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (indicator == null) return NotFound();
            if (!await ProjectBelongsToScopeAsync(indicator.ProjectID)) return Forbid();

            ViewBag.Project = indicator.Project;
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
            if (!await ProjectBelongsToScopeAsync(projectId)) return Forbid();

            ViewBag.Project = project;

            return View(new ImpactIndicatorFormViewModel
            {
                ProjectID = projectId,
                ProjectName = project.ProjectName,
                YearValues = BuildEmptyYearGrid(project)
            });
        }

        // POST: ImpactIndicators/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> Create(ImpactIndicatorFormViewModel form)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectID == form.ProjectID);

            if (project == null) return NotFound();
            if (!await ProjectBelongsToScopeAsync(form.ProjectID)) return Forbid();

            var targetValue = ValidateTargetValue(form);
            ValidateYearValues(form, project);

            if (!ModelState.IsValid)
            {
                return RedisplayForm(form, project);
            }

            var indicator = new ImpactIndicator
            {
                Name = form.Name.Trim(),
                UnitCode = form.UnitCode,
                TargetValue = targetValue,
                ProjectID = form.ProjectID
            };

            // Save the parent first so Id exists before the yearly rows reference it — a bad
            // year must not be able to leave a half-created indicator behind.
            _context.ImpactIndicators.Add(indicator);
            await _context.SaveChangesAsync();

            ApplyYearValues(indicator, form, project);
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
                .Include(i => i.YearlyValues)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (indicator == null) return NotFound();
            if (!await ProjectBelongsToScopeAsync(indicator.ProjectID)) return Forbid();

            var project = indicator.Project;
            ViewBag.Project = project;

            return View(new ImpactIndicatorFormViewModel
            {
                Id = indicator.Id,
                ProjectID = indicator.ProjectID,
                ProjectName = project.ProjectName,
                Name = indicator.Name,
                UnitCode = indicator.UnitCode,
                TargetValue = indicator.TargetValue.ToString(CultureInfo.InvariantCulture),
                YearValues = project.CoveredYears
                    .Select(year => new YearValueInput
                    {
                        Year = year,
                        // Blank for a year with no row, so "not entered" survives a round-trip
                        // through the form instead of being saved back as a real zero.
                        Value = indicator.HasValueForYear(year)
                            ? indicator.GetValueForYear(year).ToString(CultureInfo.InvariantCulture)
                            : null
                    })
                    .ToList()
            });
        }

        // POST: ImpactIndicators/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> Edit(int id, ImpactIndicatorFormViewModel form)
        {
            if (id != form.Id) return NotFound();

            var indicator = await _context.ImpactIndicators
                .Include(i => i.Project)
                .Include(i => i.YearlyValues)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (indicator == null) return NotFound();
            if (!await ProjectBelongsToScopeAsync(indicator.ProjectID)) return Forbid();

            var project = indicator.Project;

            var targetValue = ValidateTargetValue(form);
            ValidateYearValues(form, project);

            if (!ModelState.IsValid)
            {
                return RedisplayForm(form, project);
            }

            indicator.Name = form.Name.Trim();
            indicator.UnitCode = form.UnitCode;
            indicator.TargetValue = targetValue;

            ApplyYearValues(indicator, form, project);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Impact indicator updated successfully."].Value;
            return RedirectToAction(nameof(Details), new { id = indicator.Id });
        }

        // POST: ImpactIndicators/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.DeleteIndicator)]
        public async Task<IActionResult> Delete(int id)
        {
            var indicator = await _context.ImpactIndicators.FindAsync(id);
            if (indicator == null) return NotFound();
            if (!await ProjectBelongsToScopeAsync(indicator.ProjectID)) return Forbid();

            int projectId = indicator.ProjectID;

            // Yearly values go with it via cascade delete.
            _context.ImpactIndicators.Remove(indicator);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Impact indicator deleted."].Value;
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        // ─────────────────────────────── Helpers ────────────────────────────────

        /// <summary>
        /// Accepts "12.5" and "12,5" alike — Arabic-locale users type the comma. Mirrors
        /// FrameworkGoalCreateModel.TryParseDecimal and the client-side normalizeDecimal.
        /// </summary>
        private static bool TryParseDecimal(string? value, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var normalized = value.Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private static List<YearValueInput> BuildEmptyYearGrid(Project project) =>
            project.CoveredYears.Select(year => new YearValueInput { Year = year }).ToList();

        /// <summary>
        /// Parses and range-checks the target. Returns 0 and registers a ModelState error when
        /// invalid, so callers must not use the result unless ModelState.IsValid.
        /// [Required] on the property already covers the empty case.
        /// </summary>
        private double ValidateTargetValue(ImpactIndicatorFormViewModel form)
        {
            if (string.IsNullOrWhiteSpace(form.TargetValue)) return 0;

            if (!TryParseDecimal(form.TargetValue, out var parsed))
            {
                ModelState.AddModelError(
                    nameof(form.TargetValue),
                    _localizer["'{0}' is not a valid number.", form.TargetValue].Value);
                return 0;
            }

            if (parsed <= 0)
            {
                ModelState.AddModelError(
                    nameof(form.TargetValue),
                    _localizer["Target value must be greater than zero."].Value);
                return 0;
            }

            return parsed;
        }

        /// <summary>
        /// Rejects year cells that were filled in but do not parse as a number, or that parse to a
        /// negative. Silence here would look like a successful save that quietly dropped a value.
        /// Cells outside the project's range are not an error — they are simply ignored, since a
        /// stale form is the likely cause rather than user input.
        /// </summary>
        private void ValidateYearValues(ImpactIndicatorFormViewModel form, Project project)
        {
            var coveredYears = project.CoveredYears.ToHashSet();

            for (int i = 0; i < form.YearValues.Count; i++)
            {
                var entry = form.YearValues[i];

                if (!coveredYears.Contains(entry.Year)) continue;
                if (string.IsNullOrWhiteSpace(entry.Value)) continue;

                if (!TryParseDecimal(entry.Value, out var parsed))
                {
                    ModelState.AddModelError(
                        $"YearValues[{i}].Value",
                        _localizer["'{0}' is not a valid number.", entry.Value].Value);
                }
                else if (parsed < 0)
                {
                    ModelState.AddModelError(
                        $"YearValues[{i}].Value",
                        _localizer["Yearly value cannot be negative."].Value);
                }
            }
        }

        /// <summary>
        /// Reconciles the posted year grid against the stored rows:
        /// a filled cell adds or updates a row, a cleared cell deletes one, an untouched empty
        /// cell writes nothing (so "not entered" stays distinct from a recorded zero).
        /// Years outside the project's current range are skipped — both stale posts and rows
        /// stranded by a shortened project are left exactly as they are, never auto-deleted.
        /// </summary>
        private void ApplyYearValues(ImpactIndicator indicator, ImpactIndicatorFormViewModel form, Project project)
        {
            var coveredYears = project.CoveredYears.ToHashSet();

            foreach (var entry in form.YearValues)
            {
                if (!coveredYears.Contains(entry.Year)) continue;

                var existing = indicator.YearlyValues.FirstOrDefault(v => v.Year == entry.Year);
                var hasValue = TryParseDecimal(entry.Value, out var parsed);

                if (!hasValue)
                {
                    // Cleared: remove the row rather than zeroing it, so the year reverts to
                    // "not entered" and stops contributing to the cumulative total.
                    if (existing != null)
                    {
                        _context.ImpactIndicatorYearlyValues.Remove(existing);
                        indicator.YearlyValues.Remove(existing);
                    }
                    continue;
                }

                if (existing != null)
                {
                    existing.Value = parsed;
                    existing.DateRecorded = DateTime.Now;
                }
                else
                {
                    var added = new ImpactIndicatorYearlyValue
                    {
                        ImpactIndicatorId = indicator.Id,
                        Year = entry.Year,
                        Value = parsed,
                        DateRecorded = DateTime.Now
                    };
                    _context.ImpactIndicatorYearlyValues.Add(added);
                    indicator.YearlyValues.Add(added);
                }
            }
        }

        /// <summary>
        /// Re-renders the form after a validation failure. The year grid is rebuilt from the
        /// project so a post that omitted or reordered rows cannot shrink or corrupt the grid,
        /// while keeping whatever the user typed in each surviving year.
        /// </summary>
        private IActionResult RedisplayForm(ImpactIndicatorFormViewModel form, Project project)
        {
            var submitted = form.YearValues
                .GroupBy(v => v.Year)
                .ToDictionary(g => g.Key, g => g.First().Value);

            form.ProjectName = project.ProjectName;
            form.YearValues = project.CoveredYears
                .Select(year => new YearValueInput
                {
                    Year = year,
                    Value = submitted.TryGetValue(year, out var v) ? v : null
                })
                .ToList();

            ViewBag.Project = project;
            return View(form);
        }
    }
}
