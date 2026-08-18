using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class FrameworksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<FrameworksController> _localizer;
        private readonly UserManager<ApplicationUser> _userManager;

        public FrameworksController(
            ApplicationDbContext context,
            IStringLocalizer<FrameworksController> localizer,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _localizer = localizer;
            _userManager = userManager;
        }

        // Returns the MinistryCode the current user is restricted to, or null when
        // the user is an admin (unrestricted). Non-admin users without a MinistryCode
        // are restricted to no frameworks.
        private async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            if (User.IsInRole(UserRoles.SystemAdministrator))
            {
                return (true, null);
            }

            var user = await _userManager.GetUserAsync(User);
            return (false, user?.MinistryCode);
        }

        // Resolves the MinistryCode a framework should be saved with.
        // A non-admin is always forced onto their own ministry - whatever they posted is ignored,
        // so a tampered request cannot assign a strategy to another ministry.
        // An admin must pick an existing ministry explicitly.
        private async Task<(bool Ok, int? MinistryCode, string? Error)> ResolveMinistryCodeAsync(
            int? postedMinistryCode, bool isAdmin, int? scopedMinistryCode)
        {
            if (!isAdmin)
            {
                if (scopedMinistryCode is null)
                {
                    return (false, null, _localizer["You are not assigned to a ministry."].Value);
                }

                return (true, scopedMinistryCode, null);
            }

            if (postedMinistryCode is null)
            {
                return (false, null, _localizer["Please select a ministry."].Value);
            }

            var ministryExists = await _context.Ministries.AnyAsync(m => m.Code == postedMinistryCode);
            if (!ministryExists)
            {
                return (false, null, _localizer["The selected ministry does not exist."].Value);
            }

            return (true, postedMinistryCode, null);
        }

        private const string ViewModeCookieName = "FrameworksViewMode";
        private const string ViewModeMinistries = "ministries";
        private const string ViewModeStrategies = "strategies";

        // Which of the two list layouts the user gets: the ministries table or the flat list of
        // every strategy. An explicit ?viewMode= wins and is remembered; otherwise the remembered
        // choice applies; otherwise ministry users start on their own strategy list, since a
        // ministries table would only ever hold their single row.
        private string ResolveViewMode(string? requestedViewMode, bool isAdmin)
        {
            if (string.Equals(requestedViewMode, ViewModeMinistries, StringComparison.OrdinalIgnoreCase)
                || string.Equals(requestedViewMode, ViewModeStrategies, StringComparison.OrdinalIgnoreCase))
            {
                var mode = requestedViewMode!.ToLowerInvariant();

                // Only an explicit choice is persisted. Drilling into a ministry narrows the
                // page temporarily and must not overwrite the preference.
                Response.Cookies.Append(
                    ViewModeCookieName,
                    mode,
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

                return mode;
            }

            var remembered = Request.Cookies[ViewModeCookieName];
            if (remembered == ViewModeMinistries || remembered == ViewModeStrategies)
            {
                return remembered;
            }

            return isAdmin ? ViewModeMinistries : ViewModeStrategies;
        }

        // Ministry name in the active UI culture, for JSON responses the views render directly.
        private static string GetMinistryDisplayName(Ministry? ministry)
        {
            if (ministry == null)
            {
                return string.Empty;
            }

            return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar"
                ? ministry.MinistryDisplayName_AR
                : ministry.MinistryDisplayName_EN;
        }

        // Ministries to show against each framework in the list: the owning ministry plus
        // any reached through its indicators' projects. This is the same union used to scope
        // frameworks and to apply the SelectedMinistries filter above, so a badge in the
        // table always matches what clicking it filters to.
        private async Task<Dictionary<int, List<Ministry>>> BuildFrameworkMinistriesAsync(
            List<Framework> frameworks, List<Ministry> allMinistries)
        {
            var result = new Dictionary<int, List<Ministry>>();
            if (frameworks.Count == 0)
            {
                return result;
            }

            var frameworkCodes = frameworks.Select(f => f.Code).ToList();

            // Project.Ministries is not part of the Index include chain, so the indirect
            // links need their own projection query.
            var indirectLinks = await _context.Indicators
                .Where(i => i.Project != null
                    && frameworkCodes.Contains(i.SubOutput.Output.Outcome.FrameworkCode))
                .SelectMany(i => i.Project!.Ministries.Select(m => new
                {
                    FrameworkCode = i.SubOutput.Output.Outcome.FrameworkCode,
                    MinistryCode = m.Code
                }))
                .Distinct()
                .ToListAsync();

            var codesByFramework = indirectLinks
                .GroupBy(l => l.FrameworkCode)
                .ToDictionary(g => g.Key, g => g.Select(l => l.MinistryCode).ToList());

            var ministriesByCode = allMinistries
                .GroupBy(m => m.Code)
                .ToDictionary(g => g.Key, g => g.First());

            var isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            foreach (var framework in frameworks)
            {
                var codes = new List<int>();
                if (framework.MinistryCode is int owner)
                {
                    codes.Add(owner);
                }

                if (codesByFramework.TryGetValue(framework.Code, out var linked))
                {
                    codes.AddRange(linked);
                }

                result[framework.Code] = codes
                    .Distinct()
                    .Where(ministriesByCode.ContainsKey)
                    .Select(code => ministriesByCode[code])
                    .OrderBy(m => isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN)
                    .ToList();
            }

            return result;
        }

        // Loads the ministries shown in the strategy creation dropdowns, plus the
        // scope flags the views use to lock the dropdown for ministry users.
        private async Task PopulateMinistrySelectionAsync(bool isAdmin, int? scopedMinistryCode)
        {
            ViewBag.Ministries = await _context.Ministries.ToListAsync();
            ViewBag.IsMinistryUser = !isAdmin;
            ViewBag.UserMinistryCode = scopedMinistryCode;
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

            return LocalRedirect(returnUrl ?? "/");
        }

        // GET: Frameworks
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Index(string sortOrder, string searchString, FrameworkFilterViewModel filter, bool unassignedOnly = false, bool ownerlessOnly = false, string? viewMode = null)
        {
            ViewData["CurrentSort"] = sortOrder;
            // هنا قمنا بتغيير الفرز الافتراضي ليكون تنازليًا حسب أداءالمشاريع
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["IndicatorsSortParm"] = sortOrder == "indicators" ? "indicators_desc" : "indicators";
            ViewData["DisbursementSortParm"] = sortOrder == "disbursement" ? "disbursement_desc" : "disbursement";
            ViewData["CurrentFilter"] = searchString;

            // Load dropdown/filter data for the ViewModel
            filter.Ministries = await _context.Ministries.ToListAsync();
            filter.Donors = await _context.Donors.ToListAsync();
            filter.Sectors = await _context.Sectors.ToListAsync();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            filter.IsMinistryUser = !isAdmin; // non-admins are tied to one ministry -> hide the ministry filter
            ViewBag.UserMinistryCode = scopedMinistryCode; // preselects + locks the inline create dropdown

            // Ministry names come from BuildFrameworkMinistriesAsync below, so the Ministry
            // navigation does not need eager loading here.
            IQueryable<Framework> frameworksQuery = _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.Project)
                .AsQueryable();

            if (!isAdmin)
            {
                if (scopedMinistryCode is null)
                {
                    frameworksQuery = frameworksQuery.Where(f => false);
                }
                else
                {
                    // A framework belongs to the ministry if EITHER its own MinistryCode matches
                    // OR one of its indicators' projects belongs to that ministry.
                    frameworksQuery = frameworksQuery.Where(f =>
                        f.MinistryCode == scopedMinistryCode ||
                        f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so =>
                            so.Indicators.Any(i => i.Project != null &&
                                i.Project.Ministries.Any(min => min.Code == scopedMinistryCode))))));
                }
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    EF.Functions.Like(f.Name, $"%{searchString}%") ||
                    f.Outcomes.Any(o => EF.Functions.Like(o.Name, $"%{searchString}%")) ||
                    f.Outcomes.Any(o => o.Outputs.Any(op => EF.Functions.Like(op.Name, $"%{searchString}%"))) ||
                    f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => EF.Functions.Like(so.Name, $"%{searchString}%")))) ||
                    f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => EF.Functions.Like(i.Name, $"%{searchString}%"))))) ||
                    f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => i.Project != null && EF.Functions.Like(i.Project.ProjectName, $"%{searchString}%")))))
                );
            }

            if (filter.SelectedMinistries != null && filter.SelectedMinistries.Any())
            {
                // Match either the framework's own MinistryCode or its projects' ministries.
                frameworksQuery = frameworksQuery.Where(f =>
                    (f.MinistryCode != null && filter.SelectedMinistries.Contains(f.MinistryCode.Value)) ||
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.Project != null && i.Project.Ministries.Any(min => filter.SelectedMinistries.Contains(min.Code)))))));
            }

            if (filter.SelectedDonors != null && filter.SelectedDonors.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.Project != null && i.Project.Donors.Any(don => filter.SelectedDonors.Contains(don.Code)))))));
            }

            if (filter.SelectedSector != null && filter.SelectedSector.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.Project != null && i.Project.Sectors.Any(sec => filter.SelectedSector.Contains(sec.Code)))))));
            }

            // Apply sorting logic
            switch (sortOrder)
            {
                case "name_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.Name);
                    break;
                case "indicators":
                    frameworksQuery = frameworksQuery.OrderBy(f => f.IndicatorsPerformance);
                    break;
                case "indicators_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.IndicatorsPerformance);
                    break;
                case "disbursement":
                    frameworksQuery = frameworksQuery.OrderBy(f => f.DisbursementPerformance);
                    break;
                case "disbursement_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.DisbursementPerformance);
                    break;
                default:
                    // تم تغيير القيمة الافتراضية هنا
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.IndicatorsPerformance);
                    break;
            }

            filter.Frameworks = await frameworksQuery.ToListAsync();
            filter.FrameworkMinistries = await BuildFrameworkMinistriesAsync(filter.Frameworks, filter.Ministries);

            // Strategies that belong to no ministry at all - neither an owning MinistryCode
            // nor a ministry reached through their projects.
            filter.UnassignedStrategyCount = filter.Frameworks
                .Count(f => !filter.FrameworkMinistries.TryGetValue(f.Code, out var ms) || ms.Count == 0);

            filter.UnassignedOnly = unassignedOnly;
            if (unassignedOnly)
            {
                filter.Frameworks = filter.Frameworks
                    .Where(f => !filter.FrameworkMinistries.TryGetValue(f.Code, out var ms) || ms.Count == 0)
                    .ToList();
            }

            // Strategies with no OWNER, whether or not a ministry reaches them through a project.
            // The count above cannot find these: a strategy carrying another ministry's projects
            // has a non-empty badge list, so it reads as assigned while nobody actually owns it.
            filter.OwnerlessStrategyCount = filter.Frameworks.Count(f => f.MinistryCode == null);

            filter.OwnerlessOnly = ownerlessOnly;
            if (ownerlessOnly)
            {
                filter.Frameworks = filter.Frameworks.Where(f => f.MinistryCode == null).ToList();
            }

            // One row per ministry that has at least one strategy, built by inverting the
            // framework -> ministries map. A non-admin is narrowed to their own ministry: a
            // strategy shared with another ministry would otherwise surface that ministry as
            // a row on their landing page.
            var isArabicUi = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            filter.MinistryGroups = filter.FrameworkMinistries
                .SelectMany(entry => entry.Value.Select(ministry => new { FrameworkCode = entry.Key, Ministry = ministry }))
                .Where(x => isAdmin || x.Ministry.Code == scopedMinistryCode)
                .GroupBy(x => x.Ministry.Code)
                .Select(g => new MinistryStrategyGroup
                {
                    Ministry = g.First().Ministry,
                    StrategyCount = g.Select(x => x.FrameworkCode).Distinct().Count()
                })
                .OrderBy(g => isArabicUi ? g.Ministry.MinistryDisplayName_AR : g.Ministry.MinistryDisplayName_EN)
                .ToList();

            // The ministries table shows only when that layout is selected AND nothing narrows
            // the page: a search, a ministry, or the unassigned bucket always means a strategy list.
            filter.ShowMinistries = ResolveViewMode(viewMode, isAdmin) == ViewModeMinistries
                && !unassignedOnly
                && !ownerlessOnly
                && string.IsNullOrEmpty(searchString)
                && (filter.SelectedMinistries == null || !filter.SelectedMinistries.Any());

            // Build detailed search results if search string is provided
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchResults = BuildSearchResults(filter.Frameworks, searchString);
                ViewBag.SearchResults = searchResults;
                ViewBag.HasSearchResults = true;
            }

            return View(filter);
        }

        // POST: Frameworks/CreateInline - AJAX endpoint for inline creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> CreateInline(string name, int? ministryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = _localizer["Framework name is required."].Value });
                }

                // Check if framework name already exists
                var existingFramework = await _context.Frameworks.FirstOrDefaultAsync(f => f.Name.ToLower() == name.ToLower());
                if (existingFramework != null)
                {
                    return Json(new { success = false, message = _localizer["A framework with this name already exists."].Value });
                }

                var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
                var (ministryOk, resolvedMinistryCode, ministryError) =
                    await ResolveMinistryCodeAsync(ministryCode, isAdmin, scopedMinistryCode);

                if (!ministryOk)
                {
                    return Json(new { success = false, message = ministryError });
                }

                // Create new framework
                var framework = new Framework
                {
                    Name = name.Trim(),
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0,
                    MinistryCode = resolvedMinistryCode
                };

                _context.Add(framework);
                await _context.SaveChangesAsync();

                var ministry = await _context.Ministries.FindAsync(resolvedMinistryCode);

                // Return the created framework data for frontend update
                return Json(new
                {
                    success = true,
                    framework = new
                    {
                        code = framework.Code,
                        name = framework.Name,
                        indicatorsPerformance = Math.Round(framework.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(framework.DisbursementPerformance, 2),
                        ministryCode = framework.MinistryCode,
                        ministryName = GetMinistryDisplayName(ministry),
                        ministryLogo = ministry?.Logo ?? string.Empty
                    },
                    message = _localizer["Framework created successfully!"]
                });
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use ILogger here)
                return Json(new { success = false, message = _localizer["An error occurred while creating the framework. Please try again."].Value });
            }
        }

        [HttpPost]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && framework.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            framework.Name = name;
            _context.Update(framework);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // Reassigns a strategy to another ministry. Admin only: reassignment transfers
        // ownership, so a ministry user must not be able to hand their strategy over
        // to another ministry - or claim one that is not theirs.
        [HttpPost]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> UpdateMinistry(int id, int? ministryCode)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            var (isAdmin, _) = await GetScopeAsync();
            if (!isAdmin)
            {
                return Forbid();
            }

            if (ministryCode is null)
            {
                return Json(new { success = false, message = _localizer["Please select a ministry."].Value });
            }

            var ministry = await _context.Ministries.FindAsync(ministryCode);
            if (ministry == null)
            {
                return Json(new { success = false, message = _localizer["The selected ministry does not exist."].Value });
            }

            framework.MinistryCode = ministry.Code;
            _context.Update(framework);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                ministryCode = ministry.Code,
                ministryName = GetMinistryDisplayName(ministry),
                ministryLogo = ministry.Logo ?? string.Empty
            });
        }

        [HttpPost]
        [Permission(Permissions.DeleteStrategy)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && framework.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            // Collect all project IDs linked to indicators under this framework
            var projectIds = await (
                from outcome in _context.Outcomes
                join output in _context.Outputs on outcome.Code equals output.OutcomeCode
                join subOutput in _context.SubOutputs on output.Code equals subOutput.OutputCode
                join indicator in _context.Indicators on subOutput.Code equals indicator.SubOutputCode
                where outcome.FrameworkCode == id && indicator.ProjectID != null
                select indicator.ProjectID!.Value
            ).Distinct().ToListAsync();

            // Delete each linked project (handles cascade + performance recalculation)
            var monitoringService = new MonitoringService(_context);
            foreach (var projectId in projectIds)
            {
                await monitoringService.DeleteProjectAndRecalculateAsync(projectId);
            }

            _context.Frameworks.Remove(framework);
            await _context.SaveChangesAsync();

            return Ok();
        }


        private bool FrameworkExists(int id)
        {
            return _context.Frameworks.Any(e => e.Code == id);
        }

        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult>Monitoring()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            IQueryable<Framework> query = _context.Frameworks;
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(f => false)
                    : query.Where(f => f.MinistryCode == scopedMinistryCode);
            }
            return View(await query.ToListAsync());
        }

        private async Task RedistributeWeights(int subOutputCode)
        {
            var indicators = await _context.Indicators
                .Where(i => i.SubOutputCode == subOutputCode)
                .ToListAsync();

            if (indicators.Count == 0)
                return;

            double equalWeight = 100.0 / indicators.Count;

            foreach (var i in indicators)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = indicators.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                indicators.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

        // GET: Frameworks/CreateComprehensive
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> CreateComprehensive()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            await PopulateMinistrySelectionAsync(isAdmin, scopedMinistryCode);
            return View();
        }

        // POST: Frameworks/CreateComprehensive - Comprehensive framework creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> CreateComprehensive(ComprehensiveFrameworkModel model)
        {
            try
            {
                var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
                var (ministryOk, resolvedMinistryCode, ministryError) =
                    await ResolveMinistryCodeAsync(model.MinistryCode, isAdmin, scopedMinistryCode);

                if (!ministryOk)
                {
                    return Json(new { success = false, message = ministryError });
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    // Create Framework
                    var framework = new Framework
                    {
                        Name = model.FrameworkName.Trim(),
                        IndicatorsPerformance = 0,
                        DisbursementPerformance = 0,
                        MinistryCode = resolvedMinistryCode
                    };

                    _context.Frameworks.Add(framework);
                    await _context.SaveChangesAsync();

                    // Create Outcomes
                    var outcomeMapping = new Dictionary<int, int>();
                    for (int i = 0; i < model.Outcomes.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Outcomes[i].Name))
                        {
                            var outcome = new Outcome
                            {
                                Name = model.Outcomes[i].Name.Trim(),
                                FrameworkCode = framework.Code,
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0
                            };

                            _context.Outcomes.Add(outcome);
                            await _context.SaveChangesAsync();
                            outcomeMapping[i] = outcome.Code;
                        }
                    }

                    // Create Outputs
                    var outputMapping = new Dictionary<int, int>();
                    for (int i = 0; i < model.Outputs.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Outputs[i].Name) &&
                            outcomeMapping.ContainsKey(model.Outputs[i].OutcomeIndex))
                        {
                            var output = new Output
                            {
                                Name = model.Outputs[i].Name.Trim(),
                                OutcomeCode = outcomeMapping[model.Outputs[i].OutcomeIndex],
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0
                            };

                            _context.Outputs.Add(output);
                            await _context.SaveChangesAsync();
                            outputMapping[i] = output.Code;
                        }
                    }

                    // Create SubOutputs
                    var subOutputMapping = new Dictionary<int, int>();
                    for (int i = 0; i < model.SubOutputs.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.SubOutputs[i].Name) &&
                            outputMapping.ContainsKey(model.SubOutputs[i].OutputIndex))
                        {
                            var subOutput = new SubOutput
                            {
                                Name = model.SubOutputs[i].Name.Trim(),
                                OutputCode = outputMapping[model.SubOutputs[i].OutputIndex],
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0
                            };

                            _context.SubOutputs.Add(subOutput);
                            await _context.SaveChangesAsync();
                            subOutputMapping[i] = subOutput.Code;
                        }
                    }

                    // Create Indicators
                    for (int i = 0; i < model.Indicators.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Indicators[i].Name) &&
                            subOutputMapping.ContainsKey(model.Indicators[i].SubOutputIndex))
                        {
                            var indicator = new Indicator
                            {
                                Name = model.Indicators[i].Name.Trim(),
                                SubOutputCode = subOutputMapping[model.Indicators[i].SubOutputIndex],
                                Weight = model.Indicators[i].Weight > 0 ? model.Indicators[i].Weight : 100.0,
                                Target = model.Indicators[i].Target,
                                Source = model.Indicators[i].Source?.Trim() ?? string.Empty,
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0,
                                IsCommon = false,
                                Active = true,
                                TargetYear = DateTime.Now.AddYears(1),
                                Concept = string.Empty,
                                Description = string.Empty
                            };

                            _context.Indicators.Add(indicator);
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Redistribute weights for each sub-output to ensure they sum to 100%
                    foreach (var subOutputCode in subOutputMapping.Values)
                    {
                        await RedistributeWeights(subOutputCode);
                    }

                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = _localizer["Comprehensive framework created successfully!"],
                        frameworkId = framework.Code
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use ILogger here)
                return Json(new
                {
                    success = false,
                    message = _localizer["An error occurred while creating the framework. Please try again."]
                });
            }
        }

        // GET: Frameworks/ExportExcel
        [HttpGet]
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> ExportExcel(string? searchString)
        {
            var frameworks = await GetFilteredFrameworks(searchString);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["Frameworks"].Value);

            // Set RTL for Arabic
            if (isRtl)
            {
                worksheet.RightToLeft = true;
            }

            // Header row
            worksheet.Cell(1, 1).Value = _localizer["Framework Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Indicators Performance"].Value + " (%)";
            worksheet.Cell(1, 3).Value = _localizer["Disbursement Performance"].Value + " (%)";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data rows
            int row = 2;
            foreach (var framework in frameworks)
            {
                worksheet.Cell(row, 1).Value = framework.Name;
                worksheet.Cell(row, 2).Value = Math.Round(framework.IndicatorsPerformance, 2);
                worksheet.Cell(row, 3).Value = Math.Round(framework.DisbursementPerformance, 2);
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            var dataRange = worksheet.Range(1, 1, row - 1, 3);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "أطر_العمل" : "Frameworks";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Frameworks/ExportPdf
        [HttpGet]
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> ExportPdf(string? searchString)
        {
            var frameworks = await GetFilteredFrameworks(searchString);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    if (isRtl)
                    {
                        page.ContentFromRightToLeft();
                    }

                    page.Header()
                        .PaddingBottom(10)
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Medium)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(_localizer["Results Frameworks"].Value)
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);
                                col.Item().Text($"{_localizer["Generated on"].Value}: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Framework Name"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Indicators Performance"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Disbursement Performance"].Value).FontColor(Colors.White).Bold();
                            });

                            // Data rows
                            foreach (var framework in frameworks)
                            {
                                var indicatorsPerformance = Math.Round(framework.IndicatorsPerformance, 2);
                                var disbursementPerformance = Math.Round(framework.DisbursementPerformance, 2);

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(framework.Name);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text($"{indicatorsPerformance}%")
                                    .FontColor(GetPerformanceColor(indicatorsPerformance));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text($"{disbursementPerformance}%")
                                    .FontColor(GetPerformanceColor(disbursementPerformance));
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span(_localizer["Page"].Value + " ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var filePrefix = isRtl ? "أطر_العمل" : "Frameworks";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<Framework>> GetFilteredFrameworks(string? searchString)
        {
            IQueryable<Framework> query = _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.Project);

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(f => false)
                    : query.Where(f => f.MinistryCode == scopedMinistryCode);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(f =>
                    EF.Functions.Like(f.Name, $"%{searchString}%") ||
                    f.Outcomes.Any(o => EF.Functions.Like(o.Name, $"%{searchString}%")) ||
                    f.Outcomes.Any(o => o.Outputs.Any(op => EF.Functions.Like(op.Name, $"%{searchString}%"))) ||
                    f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => EF.Functions.Like(so.Name, $"%{searchString}%")))) ||
                    f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => EF.Functions.Like(i.Name, $"%{searchString}%"))))) ||
                    f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => i.Project != null && EF.Functions.Like(i.Project.ProjectName, $"%{searchString}%")))))
                );
            }

            return await query.OrderByDescending(f => f.IndicatorsPerformance).ToListAsync();
        }

        private static string GetPerformanceColor(double performance)
        {
            return performance switch
            {
                >= 75 => Colors.Green.Darken2,
                >= 50 => Colors.Orange.Darken2,
                _ => Colors.Red.Darken2
            };
        }

        private List<FrameworkSearchResultViewModel> BuildSearchResults(List<Framework> frameworks, string searchString)
        {
            var results = new List<FrameworkSearchResultViewModel>();
            var searchTerm = searchString.ToLower();

            foreach (var framework in frameworks)
            {
                var frameworkResult = new FrameworkSearchResultViewModel
                {
                    Framework = framework,
                    Matches = new List<SearchMatch>()
                };

                // Check framework name
                if (framework.Name.ToLower().Contains(searchTerm))
                {
                    frameworkResult.Matches.Add(new SearchMatch
                    {
                        Type = "Framework",
                        Name = framework.Name,
                        NavigationUrl = Url.Action("Index", "Outcomes", new { frameworkCode = framework.Code }),
                        Icon = "fas fa-sitemap",
                        ParentPath = "",
                        Code = framework.Code
                    });
                }

                // Check outcomes
                foreach (var outcome in framework.Outcomes ?? Enumerable.Empty<Outcome>())
                {
                    if (outcome.Name.ToLower().Contains(searchTerm))
                    {
                        frameworkResult.Matches.Add(new SearchMatch
                        {
                            Type = "Outcome",
                            Name = outcome.Name,
                            NavigationUrl = Url.Action("Index", "Outputs", new { frameworkCode = framework.Code, outcomeCode = outcome.Code }),
                            Icon = "fas fa-bullseye",
                            ParentPath = framework.Name,
                            Code = outcome.Code,
                            Metadata = new Dictionary<string, object>
                            {
                                { "FrameworkCode", framework.Code },
                                { "FrameworkName", framework.Name }
                            }
                        });
                    }

                    // Check outputs
                    foreach (var output in outcome.Outputs ?? Enumerable.Empty<Output>())
                    {
                        if (output.Name.ToLower().Contains(searchTerm))
                        {
                            frameworkResult.Matches.Add(new SearchMatch
                            {
                                Type = "Output",
                                Name = output.Name,
                                NavigationUrl = Url.Action("Index", "SubOutputs", new { frameworkCode = framework.Code, outputCode = output.Code }),
                                Icon = "fas fa-th-large",
                                ParentPath = $"{framework.Name} → {outcome.Name}",
                                Code = output.Code,
                                Metadata = new Dictionary<string, object>
                                {
                                    { "FrameworkCode", framework.Code },
                                    { "OutcomeCode", outcome.Code }
                                }
                            });
                        }

                        // Check suboutputs
                        foreach (var subOutput in output.SubOutputs ?? Enumerable.Empty<SubOutput>())
                        {
                            if (subOutput.Name.ToLower().Contains(searchTerm))
                            {
                                frameworkResult.Matches.Add(new SearchMatch
                                {
                                    Type = "SubOutput",
                                    Name = subOutput.Name,
                                    NavigationUrl = Url.Action("Index", "Indicators", new { frameworkCode = framework.Code, subOutputCode = subOutput.Code }),
                                    Icon = "fas fa-layer-group",
                                    ParentPath = $"{framework.Name} → {outcome.Name} → {output.Name}",
                                    Code = subOutput.Code,
                                    Metadata = new Dictionary<string, object>
                                    {
                                        { "FrameworkCode", framework.Code },
                                        { "OutputCode", output.Code }
                                    }
                                });
                            }

                            // Check indicators
                            foreach (var indicator in subOutput.Indicators ?? Enumerable.Empty<Indicator>())
                            {
                                if (indicator.Name.ToLower().Contains(searchTerm))
                                {
                                    frameworkResult.Matches.Add(new SearchMatch
                                    {
                                        Type = "Indicator",
                                        Name = indicator.Name,
                                        NavigationUrl = Url.Action("Details", "Indicators", new { id = indicator.IndicatorCode }),
                                        Icon = "fas fa-chart-line",
                                        ParentPath = $"{framework.Name} → {outcome.Name} → {output.Name} → {subOutput.Name}",
                                        Code = indicator.IndicatorCode,
                                        Metadata = new Dictionary<string, object>
                                        {
                                            { "FrameworkCode", framework.Code },
                                            { "SubOutputCode", subOutput.Code }
                                        }
                                    });
                                }

                                // Check project
                                if (indicator.Project?.ProjectName.ToLower().Contains(searchTerm) == true)
                                {
                                    frameworkResult.Matches.Add(new SearchMatch
                                    {
                                        Type = "Project",
                                        Name = indicator.Project.ProjectName,
                                        NavigationUrl = Url.Action("Details", "Projects", new { id = indicator.Project.ProjectID }),
                                        Icon = "fas fa-project-diagram",
                                        ParentPath = $"{framework.Name} → {outcome.Name} → {output.Name} → {subOutput.Name} → {indicator.Name}",
                                        Code = indicator.Project.ProjectID,
                                        Metadata = new Dictionary<string, object>
                                        {
                                            { "IndicatorCode", indicator.IndicatorCode },
                                            { "IndicatorName", indicator.Name }
                                        }
                                    });
                                }
                            }
                        }
                    }
                }

                if (frameworkResult.Matches.Any())
                {
                    results.Add(frameworkResult);
                }
            }

            return results;
        }
    }

    // Models for comprehensive framework creation
    public class ComprehensiveFrameworkModel
    {
        public string FrameworkName { get; set; } = string.Empty;
        public int? MinistryCode { get; set; }
        public List<OutcomeModel> Outcomes { get; set; } = new List<OutcomeModel>();
        public List<OutputModel> Outputs { get; set; } = new List<OutputModel>();
        public List<SubOutputModel> SubOutputs { get; set; } = new List<SubOutputModel>();
        public List<IndicatorModel> Indicators { get; set; } = new List<IndicatorModel>();
    }

    public class OutcomeModel
    {
        public string Name { get; set; } = string.Empty;
    }

    public class OutputModel
    {
        public string Name { get; set; } = string.Empty;
        public int OutcomeIndex { get; set; }
    }

    public class SubOutputModel
    {
        public string Name { get; set; } = string.Empty;
        public int OutputIndex { get; set; }
    }

    public class IndicatorModel
    {
        public string Name { get; set; } = string.Empty;
        public int SubOutputIndex { get; set; }
        public double Weight { get; set; } = 1.0;
        public int Target { get; set; }
        public string? Source { get; set; }
    }
}
