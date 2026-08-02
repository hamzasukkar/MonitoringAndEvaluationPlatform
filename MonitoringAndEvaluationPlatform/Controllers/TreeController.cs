using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        // GET: Frameworks1
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

        public async Task<IActionResult> Index2(int id)
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

        public async Task<IActionResult> GetFrameworkHierarchy(int id, bool includePhases = false)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && framework.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            Ministry? ministry = framework.MinistryCode.HasValue
                ? await _context.Ministries.FindAsync(framework.MinistryCode.Value)
                : null;
            bool isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            IQueryable<Framework> query;

            if (includePhases)
            {
                query = _context.Frameworks
                    .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                    .ThenInclude(so => so.Indicators)
                    .ThenInclude(i => i.Project)
                    .ThenInclude(p => p!.Phases);
            }
            else
            {
                query = _context.Frameworks
                    .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                    .ThenInclude(op => op.SubOutputs)
                    .ThenInclude(so => so.Indicators);
            }

            var frameworkNodes = query
                .Where(f => f.Code == id)
                .ToList()
                .SelectMany(f => new[]
                {
            new
            {
                id = $"F{f.Code}",
                pid = ministry != null ? $"M{ministry.Code}" : "",
                name = f.Name,
                type = "Framework",
                weight = 1.0,
                IndicatorsPerformance = Math.Round(f.IndicatorsPerformance, 0).ToString() + "%",
                DisbursementPerformance = Math.Round(f.DisbursementPerformance, 0).ToString() + "%"
            }
                }
                .Concat(f.Outcomes.SelectMany(o => new[]
                {
            new
            {
                id = $"O{o.Code}",
                pid = $"F{f.Code}",
                name = o.Name,
                type = "Outcome",
                weight = o.Weight,
                IndicatorsPerformance = Math.Round(o.IndicatorsPerformance, 0).ToString() + "%",
                DisbursementPerformance = Math.Round(o.DisbursementPerformance, 0).ToString() + "%"
            }
                }
                .Concat(o.Outputs.SelectMany(op => new[]
                {
            new
            {
                id = $"Op{op.Code}",
                pid = $"O{o.Code}",
                name = op.Name,
                type = "Output",
                weight = op.Weight,
                IndicatorsPerformance = Math.Round(op.IndicatorsPerformance, 0).ToString() + "%",
                DisbursementPerformance = Math.Round(op.DisbursementPerformance, 0).ToString() + "%"
            }
                }
                .Concat(op.SubOutputs.SelectMany(so => new[]
                {
            new
            {
                id = $"S{so.Code}",
                pid = $"Op{op.Code}",
                name = so.Name,
                type = "SubOutput",
                weight = so.Weight,
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
                        weight = i.Weight,
                        IndicatorsPerformance = Math.Round(i.IndicatorsPerformance, 0).ToString() + "%",
                        DisbursementPerformance = Math.Round(i.DisbursementPerformance, 0).ToString() + "%"
                    };

                    if (!includePhases || i.Project == null)
                        return new[] { indicatorNode }.AsEnumerable();

                    var phaseNodes = i.Project.Phases.Select(ph => new
                    {
                        id = $"Ph{ph.Id}",
                        pid = $"I{i.IndicatorCode}",
                        name = ph.Name,
                        type = "Phase",
                        weight = (double)ph.Weight,
                        IndicatorsPerformance = Math.Round(ph.PhasePerformance, 0).ToString() + "%",
                        DisbursementPerformance = Math.Round(ph.PhasePerformance, 0).ToString() + "%"
                    });

                    return new[] { indicatorNode }.Concat(phaseNodes).AsEnumerable();
                })))))))));

            if (ministry == null)
            {
                return Json(frameworkNodes);
            }

            var ministryNode = new
            {
                id = $"M{ministry.Code}",
                pid = "",
                name = isArabic ? ministry.MinistryDisplayName_AR : ministry.MinistryDisplayName_EN,
                type = "Ministry",
                weight = 1.0,
                IndicatorsPerformance = Math.Round(ministry.IndicatorsPerformance, 0).ToString() + "%",
                DisbursementPerformance = Math.Round(ministry.DisbursementPerformance, 0).ToString() + "%"
            };

            return Json(new object[] { ministryNode }.Concat(frameworkNodes));
        }




    }
}
