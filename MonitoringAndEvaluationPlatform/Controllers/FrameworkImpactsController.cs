using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// Framework-level impact targets ("قياس مؤشرات الأثر") and the weighted set of
    /// project-level <see cref="ImpactIndicator"/>s that drive each one.
    ///
    /// Ministry scoping and permissions follow FrameworkGoalsController exactly, since this hangs
    /// off Framework the same way. Reuses the existing Strategy permissions rather than adding new
    /// constants — PermissionAuthorizationHandler and RolePermissionService both switch over them
    /// by hand, so every new constant means editing two hand-written switches.
    /// </summary>
    [Authorize]
    public class FrameworkImpactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<FrameworkImpactsController> _localizer;
        private readonly UserManager<ApplicationUser> _userManager;

        public FrameworkImpactsController(
            ApplicationDbContext context,
            IStringLocalizer<FrameworkImpactsController> localizer,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _localizer = localizer;
            _userManager = userManager;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Ministry scoping — fails closed: a non-admin with no MinistryCode sees nothing.
        // ─────────────────────────────────────────────────────────────────────

        private async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            if (User.IsInRole(UserRoles.SystemAdministrator))
            {
                return (true, null);
            }

            var user = await _userManager.GetUserAsync(User);
            return (false, user?.MinistryCode);
        }

        private async Task<IQueryable<FrameworkImpact>> GetScopedImpactsQueryAsync()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            IQueryable<FrameworkImpact> query = _context.FrameworkImpacts;
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(fi => fi.Framework.MinistryCode == scopedMinistryCode);
            }
            return query;
        }

        private async Task<bool> FrameworkBelongsToScopeAsync(int frameworkCode)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (isAdmin) return true;
            if (scopedMinistryCode is null) return false;

            return await _context.Frameworks
                .Where(f => f.Code == frameworkCode)
                .AnyAsync(f => f.MinistryCode == scopedMinistryCode);
        }

        /// <summary>Loads one impact target with everything the views need, or null if out of scope.</summary>
        private async Task<FrameworkImpact?> LoadScopedImpactAsync(int id, bool tracked = false)
        {
            var query = await GetScopedImpactsQueryAsync();
            if (!tracked) query = query.AsNoTracking();

            return await query
                .Include(fi => fi.Framework)
                .Include(fi => fi.Indicators)
                    .ThenInclude(l => l.ImpactIndicator)
                        .ThenInclude(ii => ii.Project)
                .FirstOrDefaultAsync(fi => fi.Id == id);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Impact targets
        // ─────────────────────────────────────────────────────────────────────

        // GET: FrameworkImpacts?frameworkCode=7
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Index(int frameworkCode)
        {
            if (!await FrameworkBelongsToScopeAsync(frameworkCode)) return NotFound();

            var framework = await _context.Frameworks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Code == frameworkCode);
            if (framework == null) return NotFound();

            var query = await GetScopedImpactsQueryAsync();
            var impacts = await query
                .Where(fi => fi.FrameworkCode == frameworkCode)
                .Include(fi => fi.Indicators)
                    .ThenInclude(l => l.ImpactIndicator)
                .AsNoTracking()
                .OrderBy(fi => fi.TargetYear).ThenBy(fi => fi.Name)
                .ToListAsync();

            ViewBag.Framework = framework;
            return View(impacts);
        }

        // GET: FrameworkImpacts/Details/5
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Details(int id)
        {
            var impact = await LoadScopedImpactAsync(id);
            if (impact == null) return NotFound();

            return View(impact);
        }

        // GET: FrameworkImpacts/Create?frameworkCode=7
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> Create(int frameworkCode)
        {
            if (!await FrameworkBelongsToScopeAsync(frameworkCode)) return NotFound();

            var framework = await _context.Frameworks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Code == frameworkCode);
            if (framework == null) return NotFound();

            ViewBag.Framework = framework;
            return View(new FrameworkImpact
            {
                FrameworkCode = frameworkCode,
                BaselineYear = DateTime.Today.Year,
                TargetYear = DateTime.Today.Year + 5
            });
        }

        // POST: FrameworkImpacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> Create(
            [Bind("Name,Unit,Description,BaselineYear,BaselineValue,TargetYear,TargetValue,FrameworkCode")]
            FrameworkImpact impact)
        {
            ModelState.Remove(nameof(impact.Framework));
            ModelState.Remove(nameof(impact.Indicators));

            if (!await FrameworkBelongsToScopeAsync(impact.FrameworkCode)) return NotFound();

            var framework = await _context.Frameworks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Code == impact.FrameworkCode);
            if (framework == null) return NotFound();

            ValidateYears(impact);

            if (!ModelState.IsValid)
            {
                ViewBag.Framework = framework;
                return View(impact);
            }

            _context.FrameworkImpacts.Add(impact);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Impact target created successfully."].Value;
            return RedirectToAction(nameof(Details), new { id = impact.Id });
        }

        // GET: FrameworkImpacts/Edit/5
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> Edit(int id)
        {
            var impact = await LoadScopedImpactAsync(id);
            if (impact == null) return NotFound();

            ViewBag.Framework = impact.Framework;
            return View(impact);
        }

        // POST: FrameworkImpacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Unit,Description,BaselineYear,BaselineValue,TargetYear,TargetValue,FrameworkCode")]
            FrameworkImpact impact)
        {
            if (id != impact.Id) return NotFound();

            ModelState.Remove(nameof(impact.Framework));
            ModelState.Remove(nameof(impact.Indicators));

            var query = await GetScopedImpactsQueryAsync();
            var existing = await query
                .Include(fi => fi.Framework)
                .FirstOrDefaultAsync(fi => fi.Id == id);
            if (existing == null) return NotFound();

            ValidateYears(impact);

            if (!ModelState.IsValid)
            {
                ViewBag.Framework = existing.Framework;
                return View(impact);
            }

            existing.Name = impact.Name;
            existing.Unit = impact.Unit;
            existing.Description = impact.Description;
            existing.BaselineYear = impact.BaselineYear;
            existing.BaselineValue = impact.BaselineValue;
            existing.TargetYear = impact.TargetYear;
            existing.TargetValue = impact.TargetValue;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Impact target updated successfully."].Value;
            return RedirectToAction(nameof(Details), new { id = existing.Id });
        }

        // POST: FrameworkImpacts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.DeleteStrategy)]
        public async Task<IActionResult> Delete(int id)
        {
            var query = await GetScopedImpactsQueryAsync();
            var impact = await query.FirstOrDefaultAsync(fi => fi.Id == id);
            if (impact == null) return NotFound();

            int frameworkCode = impact.FrameworkCode;

            // The links cascade; the ImpactIndicators themselves are untouched.
            _context.FrameworkImpacts.Remove(impact);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Impact target deleted. The indicators themselves were not affected."].Value;
            return RedirectToAction(nameof(Index), new { frameworkCode });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Indicator selection
        // ─────────────────────────────────────────────────────────────────────

        // GET: FrameworkImpacts/SelectIndicators/5
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> SelectIndicators(int id)
        {
            var impact = await LoadScopedImpactAsync(id);
            if (impact == null) return NotFound();

            ViewBag.Impact = impact;
            ViewBag.Available = await GetAvailableIndicatorsAsync(impact.FrameworkCode);
            ViewBag.LinkedIds = impact.Indicators.Select(l => l.ImpactIndicatorId).ToHashSet();

            return View();
        }

        // POST: FrameworkImpacts/SelectIndicators/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> SelectIndicators(int id, List<int>? selectedIndicatorIds)
        {
            var query = await GetScopedImpactsQueryAsync();
            var impact = await query
                .Include(fi => fi.Indicators)
                .FirstOrDefaultAsync(fi => fi.Id == id);
            if (impact == null) return NotFound();

            var selected = (selectedIndicatorIds ?? new List<int>()).Distinct().ToList();

            // Only accept indicators this framework can actually reach — a hand-crafted POST must
            // not be able to attach an indicator from another ministry's framework.
            var allowedIds = (await GetAvailableIndicatorsAsync(impact.FrameworkCode))
                .Select(ii => ii.Id).ToHashSet();
            selected = selected.Where(allowedIds.Contains).ToList();

            var currentIds = impact.Indicators.Select(l => l.ImpactIndicatorId).ToHashSet();

            var toRemove = impact.Indicators.Where(l => !selected.Contains(l.ImpactIndicatorId)).ToList();
            if (toRemove.Any()) _context.FrameworkImpactIndicators.RemoveRange(toRemove);

            foreach (var indicatorId in selected.Where(sid => !currentIds.Contains(sid)))
            {
                _context.FrameworkImpactIndicators.Add(new FrameworkImpactIndicator
                {
                    FrameworkImpactId = impact.Id,
                    ImpactIndicatorId = indicatorId,
                    Weight = 0
                });
            }

            await _context.SaveChangesAsync();
            await RedistributeWeightsEquallyAsync(impact.Id);

            TempData["SuccessMessage"] = _localizer["{0} indicator(s) linked. Weights were redistributed equally.", selected.Count].Value;
            return RedirectToAction(nameof(Details), new { id = impact.Id });
        }

        // POST: FrameworkImpacts/UpdateWeights  (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> UpdateWeights([FromBody] UpdateWeightsDto dto)
        {
            if (dto?.Links == null || !dto.Links.Any())
                return BadRequest(new { message = _localizer["No weight data provided."].Value });

            var query = await GetScopedImpactsQueryAsync();
            var impact = await query
                .Include(fi => fi.Indicators)
                .FirstOrDefaultAsync(fi => fi.Id == dto.ImpactId);
            if (impact == null) return NotFound();

            if (dto.Links.Any(l => l.Weight < 0))
                return BadRequest(new { message = _localizer["All weights must be non-negative."].Value });

            // Enforced here as well as in the browser — a disabled button is trivial to bypass.
            var total = dto.Links.Sum(l => l.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
                return BadRequest(new
                {
                    message = _localizer["Weights must sum to exactly 100. Current sum: {0}.", total.ToString("0.##")].Value
                });

            foreach (var item in dto.Links)
            {
                var link = impact.Indicators.FirstOrDefault(l => l.Id == item.LinkId);
                if (link != null) link.Weight = Math.Round(item.Weight, 2);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = _localizer["Weights updated successfully."].Value });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Impact indicators reachable from a framework. They hang off projects, not frameworks,
        /// so the path is Framework → Outcome → Output → SubOutput → Indicator → Project →
        /// ImpactIndicator. Grouped by project in the UI because this list grows with the number
        /// of projects under the framework.
        /// </summary>
        private async Task<List<ImpactIndicator>> GetAvailableIndicatorsAsync(int frameworkCode)
        {
            return await _context.ImpactIndicators
                .Include(ii => ii.Project)
                .Where(ii => _context.Indicators.Any(i =>
                    i.ProjectID == ii.ProjectID &&
                    i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode))
                .AsNoTracking()
                .OrderBy(ii => ii.Project.ProjectName).ThenBy(ii => ii.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Equal weights summing to exactly 100, with the rounding remainder absorbed by the last
        /// row — the same approach as IndicatorProjectPairService.RedistributeIndicatorWeightsAsync.
        /// </summary>
        private async Task RedistributeWeightsEquallyAsync(int impactId)
        {
            var links = await _context.FrameworkImpactIndicators
                .Where(l => l.FrameworkImpactId == impactId)
                .OrderBy(l => l.Id)
                .ToListAsync();

            if (links.Count == 0) return;

            double equalWeight = Math.Round(100.0 / links.Count, 2);
            foreach (var link in links) link.Weight = equalWeight;

            double total = links.Sum(l => l.Weight);
            if (Math.Abs(total - 100.0) > 0.001)
            {
                links[^1].Weight = Math.Round(links[^1].Weight + (100.0 - total), 2);
            }

            await _context.SaveChangesAsync();
        }

        private void ValidateYears(FrameworkImpact impact)
        {
            if (impact.BaselineYear >= impact.TargetYear)
            {
                ModelState.AddModelError(nameof(impact.TargetYear),
                    _localizer["Target year must be after the baseline year."].Value);
            }
        }

        // DTOs for AJAX
        public class UpdateWeightsDto
        {
            public int ImpactId { get; set; }
            public List<WeightItem> Links { get; set; } = new();
        }

        public class WeightItem
        {
            public int LinkId { get; set; }
            public double Weight { get; set; }
        }
    }
}
