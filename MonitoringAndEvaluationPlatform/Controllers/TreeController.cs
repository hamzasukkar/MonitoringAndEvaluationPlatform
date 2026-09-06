using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class TreeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TreeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            if (User.IsInRole(UserRoles.SystemAdministrator))
            {
                return (true, null);
            }

            var user = await _userManager.GetUserAsync(User);
            return (false, user?.MinistryCode);
        }

        // Frameworks a ministry owns or reaches through its projects. Same union as the
        // SelectedMinistries filter in FrameworksController.Index, so the tree shows the
        // same strategies as the count badge on the ministry row the user clicked.
        private static IQueryable<Framework> ScopeToMinistries(IQueryable<Framework> query, List<int> ministryCodes)
        {
            return query.Where(f =>
                (f.MinistryCode != null && ministryCodes.Contains(f.MinistryCode.Value)) ||
                f.Outcomes.Any(o =>
                    o.Outputs.Any(op =>
                        op.SubOutputs.Any(so =>
                            so.Indicators.Any(i =>
                                i.Project != null &&
                                i.Project.Ministries.Any(m => ministryCodes.Contains(m.Code)))))));
        }

        private IQueryable<Framework> BuildHierarchyQuery(bool includePhases)
        {
            if (includePhases)
            {
                return _context.Frameworks
                    .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                    .ThenInclude(so => so.Indicators)
                    .ThenInclude(i => i.Project)
                    .ThenInclude(p => p!.Phases);
            }

            return _context.Frameworks
                .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                .ThenInclude(op => op.SubOutputs)
                .ThenInclude(so => so.Indicators);
        }

        // A ministry user may only ever look at their own ministry. Shared by the page
        // action and the data endpoint so the two cannot drift apart.
        private static bool IsMinistrySelectionAllowed(bool isAdmin, int? scopedMinistryCode, List<int> ministryCodes)
        {
            return isAdmin || (scopedMinistryCode.HasValue && ministryCodes.All(c => c == scopedMinistryCode.Value));
        }

        private static string MinistryDisplayName(Ministry ministry, bool isArabic)
        {
            return isArabic ? ministry.MinistryDisplayName_AR : ministry.MinistryDisplayName_EN;
        }

        private static object BuildMinistryNode(Ministry ministry, bool isArabic)
        {
            return new
            {
                id = $"M{ministry.Code}",
                pid = "",
                name = MinistryDisplayName(ministry, isArabic),
                type = "Ministry",
                weight = 1.0,
                IndicatorsPerformance = Math.Round(ministry.IndicatorsPerformance, 0).ToString() + "%",
                DisbursementPerformance = Math.Round(ministry.DisbursementPerformance, 0).ToString() + "%"
            };
        }

        // The framework node plus its whole subtree, flattened to the id/pid shape the
        // client's buildHierarchy() expects. Property names here are the client contract.
        private static IEnumerable<object> BuildFrameworkNodes(Framework f, string parentId, bool includePhases)
        {
            return new object[]
            {
                new
                {
                    id = $"F{f.Code}",
                    pid = parentId,
                    name = f.Name,
                    type = "Framework",
                    weight = 1.0,
                    IndicatorsPerformance = Math.Round(f.IndicatorsPerformance, 0).ToString() + "%",
                    DisbursementPerformance = Math.Round(f.DisbursementPerformance, 0).ToString() + "%"
                }
            }
            .Concat(f.Outcomes.SelectMany(o => new object[]
            {
                new
                {
                    id = $"O{o.Code}",
                    pid = $"F{f.Code}",
                    name = o.Name,
                    type = "Outcome",
                    weight = Math.Round(o.Weight, 2),
                    IndicatorsPerformance = Math.Round(o.IndicatorsPerformance, 0).ToString() + "%",
                    DisbursementPerformance = Math.Round(o.DisbursementPerformance, 0).ToString() + "%"
                }
            }
            .Concat(o.Outputs.SelectMany(op => new object[]
            {
                new
                {
                    id = $"Op{op.Code}",
                    pid = $"O{o.Code}",
                    name = op.Name,
                    type = "Output",
                    weight = Math.Round(op.Weight, 2),
                    IndicatorsPerformance = Math.Round(op.IndicatorsPerformance, 0).ToString() + "%",
                    DisbursementPerformance = Math.Round(op.DisbursementPerformance, 0).ToString() + "%"
                }
            }
            .Concat(op.SubOutputs.SelectMany(so => new object[]
            {
                new
                {
                    id = $"S{so.Code}",
                    pid = $"Op{op.Code}",
                    name = so.Name,
                    type = "SubOutput",
                    weight = Math.Round(so.Weight, 2),
                    IndicatorsPerformance = Math.Round(so.IndicatorsPerformance, 0).ToString() + "%",
                    DisbursementPerformance = Math.Round(so.DisbursementPerformance, 0).ToString() + "%"
                }
            }
            .Concat(so.Indicators.SelectMany(i =>
            {
                var indicatorNode = new
                {
                    id = $"I{i.IndicatorCode}",
                    pid = $"S{so.Code}",
                    name = i.Name,
                    type = "Indicator",
                    weight = Math.Round(i.Weight, 2),
                    IndicatorsPerformance = Math.Round(i.IndicatorsPerformance, 0).ToString() + "%",
                    DisbursementPerformance = Math.Round(i.DisbursementPerformance, 0).ToString() + "%"
                };

                if (!includePhases || i.Project == null)
                    return new object[] { indicatorNode }.AsEnumerable();

                var phaseNodes = i.Project.Phases.Select(ph => (object)new
                {
                    id = $"Ph{ph.Id}",
                    pid = $"I{i.IndicatorCode}",
                    name = ph.Name,
                    type = "Phase",
                    weight = (double)ph.Weight,
                    IndicatorsPerformance = Math.Round(ph.PhasePerformance, 0).ToString() + "%",
                    DisbursementPerformance = Math.Round(ph.PhasePerformance, 0).ToString() + "%"
                });

                return new object[] { indicatorNode }.Concat(phaseNodes).AsEnumerable();
            }))))))));
        }

        // Each framework hangs off exactly one ministry root: node ids are unique keys in
        // buildHierarchy(), so a strategy shared by two selected ministries must not be
        // emitted twice. Its owning ministry wins; otherwise the lowest-code selected
        // ministry it reaches through a project.
        private async Task<Dictionary<int, int>> ResolveFrameworkParentsAsync(
            List<Framework> frameworks, List<int> ministryCodes)
        {
            var parents = new Dictionary<int, int>();

            if (ministryCodes.Count == 1)
            {
                foreach (var framework in frameworks)
                {
                    parents[framework.Code] = ministryCodes[0];
                }

                return parents;
            }

            var frameworkCodes = frameworks.Select(f => f.Code).ToList();

            // Project.Ministries is not part of the hierarchy include chain, so the
            // indirect links need their own projection query.
            var links = await _context.Indicators
                .Where(i => i.Project != null
                    && frameworkCodes.Contains(i.SubOutput.Output.Outcome.FrameworkCode))
                .SelectMany(i => i.Project!.Ministries
                    .Where(m => ministryCodes.Contains(m.Code))
                    .Select(m => new
                    {
                        FrameworkCode = i.SubOutput.Output.Outcome.FrameworkCode,
                        MinistryCode = m.Code
                    }))
                .Distinct()
                .ToListAsync();

            var linkedByFramework = links
                .GroupBy(l => l.FrameworkCode)
                .ToDictionary(g => g.Key, g => g.Select(l => l.MinistryCode).Min());

            foreach (var framework in frameworks)
            {
                if (framework.MinistryCode is int owner && ministryCodes.Contains(owner))
                {
                    parents[framework.Code] = owner;
                }
                else if (linkedByFramework.TryGetValue(framework.Code, out var linked))
                {
                    parents[framework.Code] = linked;
                }
            }

            return parents;
        }

        // GET: Frameworks1
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Index(int id)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && framework.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            ViewData["FrameworkCode"] = id;
            return View();
        }

        // Two modes: a single strategy (/Tree/Index2/5, the framework tree) or a whole
        // ministry (/Tree/Index2?SelectedMinistries=5, the ministry and every strategy
        // it owns or reaches through its projects).
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Index2(int id, [FromQuery] List<int>? SelectedMinistries = null)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var ministryCodes = SelectedMinistries?.Distinct().ToList() ?? new List<int>();

            if (ministryCodes.Count > 0)
            {
                if (!IsMinistrySelectionAllowed(isAdmin, scopedMinistryCode, ministryCodes))
                {
                    return Forbid();
                }

                var ministries = await _context.Ministries
                    .Where(m => ministryCodes.Contains(m.Code))
                    .ToListAsync();

                if (ministries.Count == 0) return NotFound();

                var isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

                ViewData["FrameworkCode"] = null;
                ViewData["MinistryCodesJson"] = "[" + string.Join(",", ministries.Select(m => m.Code)) + "]";
                ViewData["TreeSubject"] = string.Join("، ", ministries
                    .Select(m => MinistryDisplayName(m, isArabic))
                    .OrderBy(n => n));

                return View();
            }

            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            if (!isAdmin && framework.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            ViewData["FrameworkCode"] = id;
            ViewData["MinistryCodesJson"] = "[]";
            return View();
        }

        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> GetFrameworkHierarchy(
            int id, bool includePhases = false, [FromQuery] List<int>? SelectedMinistries = null)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            bool isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            var ministryCodes = SelectedMinistries?.Distinct().ToList() ?? new List<int>();

            if (ministryCodes.Count > 0)
            {
                if (!IsMinistrySelectionAllowed(isAdmin, scopedMinistryCode, ministryCodes))
                {
                    return Forbid();
                }

                var ministries = await _context.Ministries
                    .Where(m => ministryCodes.Contains(m.Code))
                    .ToListAsync();

                if (ministries.Count == 0) return NotFound();

                var resolvedCodes = ministries.Select(m => m.Code).ToList();

                var frameworks = await ScopeToMinistries(BuildHierarchyQuery(includePhases), resolvedCodes)
                    .ToListAsync();

                var parents = await ResolveFrameworkParentsAsync(frameworks, resolvedCodes);

                // A ministry with no strategies still renders as a lone root node.
                var ministryNodes = ministries.Select(m => BuildMinistryNode(m, isArabic));

                var nodes = frameworks
                    .Where(f => parents.ContainsKey(f.Code))
                    .SelectMany(f => BuildFrameworkNodes(f, $"M{parents[f.Code]}", includePhases));

                return Json(ministryNodes.Concat(nodes));
            }

            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            if (!isAdmin && framework.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            Ministry? ministry = framework.MinistryCode.HasValue
                ? await _context.Ministries.FindAsync(framework.MinistryCode.Value)
                : null;

            var singleFramework = await BuildHierarchyQuery(includePhases)
                .Where(f => f.Code == id)
                .ToListAsync();

            var frameworkNodes = singleFramework.SelectMany(f =>
                BuildFrameworkNodes(f, ministry != null ? $"M{ministry.Code}" : "", includePhases));

            if (ministry == null)
            {
                return Json(frameworkNodes);
            }

            return Json(new[] { BuildMinistryNode(ministry, isArabic) }.Concat(frameworkNodes));
        }
    }
}
