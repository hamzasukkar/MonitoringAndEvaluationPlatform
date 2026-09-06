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
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class MonitoringController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMinistryStatisticsService _ministryStatistics;

        public MonitoringController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IMinistryStatisticsService ministryStatistics)
        {
            _context = context;
            _userManager = userManager;
            _ministryStatistics = ministryStatistics;
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

        public async Task<IActionResult> FrameworkDashboard(List<int> selectedMinistryIds)
        {
            // 1) Load all ministries (for the filter dropdown, etc.)
            var allMinistries = _context.Ministries.ToList();

            // 2) Start from Frameworks, but now eagerly include Project.Ministries
            var frameworks = _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(outp => outp.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.Project)
                                    .ThenInclude(p => p.Ministries)
                .AsQueryable();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                frameworks = scopedMinistryCode is null
                    ? frameworks.Where(_ => false)
                    : frameworks.Where(f => f.MinistryCode == scopedMinistryCode);
            }

            if (selectedMinistryIds != null && selectedMinistryIds.Any())
            {
                frameworks = frameworks
                    .Where(f =>
                        f.Outcomes
                         .SelectMany(o => o.Outputs)
                         .SelectMany(outp => outp.SubOutputs)
                         .SelectMany(so => so.Indicators)
                         .Any(i => i.Project != null && i.Project.Ministries.Any(min => selectedMinistryIds.Contains(min.Code))
                         )
                    );
            }

            var viewModel = new FrameworkDashboardViewModel
            {
                Frameworks = frameworks.ToList(),
                Ministries = allMinistries,
                SelectedMinistryIds = selectedMinistryIds
            };

            return View(viewModel);
        }


        /// <summary>
        /// The top of the Monitoring drill-down: one card per ministry, sitting directly above the
        /// framework cards on Index. Ministry became a level of the hierarchy
        /// (Ministry -> Strategy -> Outcome -> ... -> Project), and this is Monitoring's view of it.
        ///
        /// Metrics come from IMinistryStatisticsService, the same source the Dashboard ministry tier
        /// and the Ministry Report read, so the three surfaces cannot drift apart.
        /// </summary>
        public async Task<IActionResult> Ministry(int? ministryCode, CancellationToken cancellationToken = default)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();

            // Fail closed, matching every other action here.
            if (!isAdmin && scopedMinistryCode is null)
            {
                return View(new MinistryMonitoringViewModel());
            }

            // The argument is DISCARDED for a non-admin rather than combined with their scope, so a
            // hand-edited ?ministryCode= can never widen what they see.
            var requested = isAdmin ? ministryCode : scopedMinistryCode;

            var stats = await _ministryStatistics.GetAsync(requested, cancellationToken: cancellationToken);
            var visible = stats.Select(s => s.Code).ToList();

            if (visible.Count == 0)
            {
                return View(new MinistryMonitoringViewModel());
            }

            // Hierarchy counts, down the strategy-ownership path so they match the Outcome/Outputs/
            // SubOutputs links. Projected per strategy and grouped in memory on purpose: a
            // GroupBy(f => f.MinistryCode).Select(g => g.SelectMany(...).Count()) does not translate
            // in EF Core 8 and fails at runtime rather than at compile time.
            var perStrategy = await _context.Frameworks
                .AsNoTracking()
                .Where(f => f.MinistryCode != null && visible.Contains(f.MinistryCode.Value))
                .Select(f => new
                {
                    MinistryCode = f.MinistryCode!.Value,
                    OutcomeCount = f.Outcomes.Count,
                    OutputCount = f.Outcomes.SelectMany(o => o.Outputs).Count(),
                    SubOutputCount = f.Outcomes.SelectMany(o => o.Outputs).SelectMany(op => op.SubOutputs).Count()
                })
                .ToListAsync(cancellationToken);

            var hierarchy = perStrategy
                .GroupBy(x => x.MinistryCode)
                .ToDictionary(
                    g => g.Key,
                    g => (Outcomes: g.Sum(x => x.OutcomeCount),
                          Outputs: g.Sum(x => x.OutputCount),
                          SubOutputs: g.Sum(x => x.SubOutputCount)));

            // Phase counts, down the project-ownership path (either ministry edge) so they match the
            // Phase link and Stats.ProjectCount. One query, grouped in memory.
            var phaseRows = await _context.ProjectPhases
                .AsNoTracking()
                .Where(pp => (pp.Project.MinistryCode != null && visible.Contains(pp.Project.MinistryCode.Value))
                             || pp.Project.Ministries.Any(m => visible.Contains(m.Code)))
                .Select(pp => new
                {
                    pp.Project.MinistryCode,
                    Codes = pp.Project.Ministries.Select(m => m.Code).ToList()
                })
                .ToListAsync(cancellationToken);

            var isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            var model = new MinistryMonitoringViewModel
            {
                Cards = stats
                    .Select(s =>
                    {
                        var counts = hierarchy.TryGetValue(s.Code, out var h) ? h : default;
                        return new MinistryMonitoringCard
                        {
                            Stats = s,
                            OutcomeCount = counts.Outcomes,
                            OutputCount = counts.Outputs,
                            SubOutputCount = counts.SubOutputs,
                            PhaseCount = phaseRows.Count(r => r.MinistryCode == s.Code || r.Codes.Contains(s.Code))
                        };
                    })
                    .OrderByDescending(c => c.PerformanceValue)
                    .ThenBy(c => c.Stats.DisplayName(isArabic))
                    .ToList()
            };

            return View(model);
        }

        // GET: Monitoring
        public async Task<IActionResult> Index(int? frameworkCode, int? ministryCode)
        {
            var query = _context.Frameworks
                .Include(i => i.Outcomes)
                .ThenInclude(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .ThenInclude(i => i.Project)
                    .ThenInclude(p => p.Phases)
                .AsQueryable();

            // Ministry scope and the ministry FILTER are one predicate: the query-string value is
            // honoured only for an admin, so a scoped user's ministryCode argument is discarded
            // rather than combined -- widening by hand-editing the URL is impossible by construction.
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var effectiveMinistryCode = isAdmin ? ministryCode : scopedMinistryCode;

            if (!isAdmin && scopedMinistryCode is null)
            {
                query = query.Where(_ => false);
            }
            else if (effectiveMinistryCode is int scopedOrFiltered)
            {
                query = query.Where(f => f.MinistryCode == scopedOrFiltered);
            }

            ViewBag.MinistryCode = ministryCode;

            var frameworks = await query.OrderByDescending(f => f.IndicatorsPerformance).ToListAsync();
            return View(frameworks);
        }

        public async Task<IActionResult> Outcome(int? frameworkCode, int? ministryCode)
        {
            var outcomesQuery = _context.Outcomes
                .Include(o => o.Outputs)
                    .ThenInclude(ou => ou.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                            .ThenInclude(i => i.Project)
                                .ThenInclude(p => p.Phases)
                .AsQueryable();

            if (frameworkCode.HasValue)
            {
                outcomesQuery = outcomesQuery.Where(o => o.FrameworkCode == frameworkCode.Value);
            }

            // Ministry scope and the ministry FILTER are one predicate: the query-string value is
            // honoured only for an admin, so a scoped user's ministryCode argument is discarded
            // rather than combined -- widening by hand-editing the URL is impossible by construction.
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var effectiveMinistryCode = isAdmin ? ministryCode : scopedMinistryCode;

            if (!isAdmin && scopedMinistryCode is null)
            {
                outcomesQuery = outcomesQuery.Where(_ => false);
            }
            else if (effectiveMinistryCode is int scopedOrFiltered)
            {
                outcomesQuery = outcomesQuery.Where(o => o.Framework.MinistryCode == scopedOrFiltered);
            }

            ViewBag.MinistryCode = ministryCode;

            var outcomes = await outcomesQuery.ToListAsync();

            // Dictionary: OutcomeCode -> Distinct Project Count
            var projectCounts = outcomes.ToDictionary(
                o => o.Code,
                o => o.Outputs
                        .SelectMany(ou => ou.SubOutputs)
                        .SelectMany(so => so.Indicators)
                        .Where(i => i.ProjectID != null)
                        .Select(i => i.ProjectID)
                        .Distinct()
                        .Count()
            );

            // Dictionary: OutcomeCode -> Distinct Phase Count
            var phaseCounts = outcomes.ToDictionary(
                o => o.Code,
                o => o.Outputs
                        .SelectMany(ou => ou.SubOutputs)
                        .SelectMany(so => so.Indicators)
                        .Where(i => i.Project != null)
                        .SelectMany(i => i.Project!.Phases)
                        .Select(ph => ph.Id)
                        .Distinct()
                        .Count()
            );

            ViewBag.ProjectCounts = projectCounts;
            ViewBag.PhaseCounts = phaseCounts;

            return View(outcomes);
        }


        public async Task<IActionResult> Outputs(int? frameworkCode, int? outcomeCode, int? ministryCode)
        {
            var outputsQuery = _context.Outputs
                .Include(o => o.SubOutputs)
                    .ThenInclude(so => so.Indicators)
                        .ThenInclude(i => i.Project)
                            .ThenInclude(p => p.Phases)
                .Include(o => o.Outcome)
                    .ThenInclude(oc => oc.Framework)
                .AsQueryable();

            if (frameworkCode.HasValue)
            {
                outputsQuery = outputsQuery.Where(o => o.Outcome.FrameworkCode == frameworkCode.Value);
            }

            if (outcomeCode.HasValue)
            {
                outputsQuery = outputsQuery.Where(o => o.OutcomeCode == outcomeCode.Value);
            }

            // Ministry scope and the ministry FILTER are one predicate: the query-string value is
            // honoured only for an admin, so a scoped user's ministryCode argument is discarded
            // rather than combined -- widening by hand-editing the URL is impossible by construction.
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var effectiveMinistryCode = isAdmin ? ministryCode : scopedMinistryCode;

            if (!isAdmin && scopedMinistryCode is null)
            {
                outputsQuery = outputsQuery.Where(_ => false);
            }
            else if (effectiveMinistryCode is int scopedOrFiltered)
            {
                outputsQuery = outputsQuery.Where(o => o.Outcome.Framework.MinistryCode == scopedOrFiltered);
            }

            ViewBag.MinistryCode = ministryCode;

            var outputs = await outputsQuery.ToListAsync();

            // Dictionary: OutputCode -> Distinct Project Count
            var projectCounts = outputs.ToDictionary(
                o => o.Code,
                o => o.SubOutputs
                        .SelectMany(so => so.Indicators)
                        .Where(i => i.ProjectID != null)
                        .Select(i => i.ProjectID)
                        .Distinct()
                        .Count()
            );

            // Dictionary: OutputCode -> Distinct Phase Count
            var phaseCounts = outputs.ToDictionary(
                o => o.Code,
                o => o.SubOutputs
                        .SelectMany(so => so.Indicators)
                        .Where(i => i.Project != null)
                        .SelectMany(i => i.Project!.Phases)
                        .Select(ph => ph.Id)
                        .Distinct()
                        .Count()
            );

            ViewBag.ProjectCounts = projectCounts;
            ViewBag.PhaseCounts = phaseCounts;

            return View(outputs);
        }


        public async Task<IActionResult> SubOutputs(int? frameworkCode, int? outcomeCode, int? outputCode, int? ministryCode)
        {
            var subOutputsQuery = _context.SubOutputs
                .Include(so => so.Indicators)
                    .ThenInclude(i => i.Project)
                        .ThenInclude(p => p.Phases)
                .Include(so => so.Output)
                    .ThenInclude(o => o.Outcome)
                        .ThenInclude(oc => oc.Framework)
                .AsQueryable();

            if (frameworkCode.HasValue)
            {
                subOutputsQuery = subOutputsQuery.Where(so => so.Output.Outcome.FrameworkCode == frameworkCode.Value);
            }

            if (outcomeCode.HasValue)
            {
                subOutputsQuery = subOutputsQuery.Where(so => so.Output.OutcomeCode == outcomeCode.Value);
            }

            if (outputCode.HasValue)
            {
                subOutputsQuery = subOutputsQuery.Where(so => so.OutputCode == outputCode.Value);
            }

            // Ministry scope and the ministry FILTER are one predicate: the query-string value is
            // honoured only for an admin, so a scoped user's ministryCode argument is discarded
            // rather than combined -- widening by hand-editing the URL is impossible by construction.
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var effectiveMinistryCode = isAdmin ? ministryCode : scopedMinistryCode;

            if (!isAdmin && scopedMinistryCode is null)
            {
                subOutputsQuery = subOutputsQuery.Where(_ => false);
            }
            else if (effectiveMinistryCode is int scopedOrFiltered)
            {
                subOutputsQuery = subOutputsQuery.Where(so => so.Output.Outcome.Framework.MinistryCode == scopedOrFiltered);
            }

            ViewBag.MinistryCode = ministryCode;

            var subOutputs = await subOutputsQuery.ToListAsync();

            // Dictionary: SubOutputCode -> Distinct Project Count
            var projectCounts = subOutputs.ToDictionary(
                so => so.Code,
                so => so.Indicators
                        .Where(i => i.ProjectID != null)
                        .Select(i => i.ProjectID)
                        .Distinct()
                        .Count()
            );

            // Dictionary: SubOutputCode -> Distinct Phase Count
            var phaseCounts = subOutputs.ToDictionary(
                so => so.Code,
                so => so.Indicators
                        .Where(i => i.Project != null)
                        .SelectMany(i => i.Project!.Phases)
                        .Select(ph => ph.Id)
                        .Distinct()
                        .Count()
            );

            ViewBag.ProjectCounts = projectCounts;
            ViewBag.PhaseCounts = phaseCounts;

            return View(subOutputs);
        }



        public async Task<IActionResult> Projects(
            int? frameworkCode,
            int? outcomeCode,
            int? outputCode,
            int? subOutputCode,
            int? indicatorCode,
            int? ministryCode,
            string? search)
        {
            // Query projects linked via Indicator.ProjectID
            var projectsQuery = _context.Projects
                .Include(p => p.Sector)
                .Include(p => p.Donors)
                .Include(p => p.Ministries)
                .Include(p => p.Communities)
                .AsQueryable();

            if (indicatorCode.HasValue)
            {
                projectsQuery = projectsQuery.Where(p => p.Indicators.Any(i => i.IndicatorCode == indicatorCode.Value));
            }
            else if (subOutputCode.HasValue)
            {
                projectsQuery = projectsQuery.Where(p => p.Indicators.Any(i => i.SubOutputCode == subOutputCode.Value));
            }
            else if (outputCode.HasValue)
            {
                projectsQuery = projectsQuery.Where(p => p.Indicators.Any(i => i.SubOutput.OutputCode == outputCode.Value));
            }
            else if (outcomeCode.HasValue)
            {
                projectsQuery = projectsQuery.Where(p => p.Indicators.Any(i => i.SubOutput.Output.OutcomeCode == outcomeCode.Value));
            }
            else if (frameworkCode.HasValue)
            {
                projectsQuery = projectsQuery.Where(p => p.Indicators.Any(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode.Value));
            }

            // Ministry scope and the ministry FILTER are one predicate: the query-string value is
            // honoured only for an admin, so a scoped user's ministryCode argument is discarded
            // rather than combined -- widening by hand-editing the URL is impossible by construction.
            //
            // The predicate matches on EITHER ministry edge, the same rule as
            // MinistryStatisticsService.BelongsTo. This filter previously used the ProjectMinistries
            // join alone while the scope above used the MinistryCode FK, so a project attached by
            // only one edge was scoped in but filtered out -- and the count disagreed with the
            // ministry card, the Dashboard tier and the Ministry Report, which all use the union.
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var effectiveMinistryCode = isAdmin ? ministryCode : scopedMinistryCode;

            if (!isAdmin && scopedMinistryCode is null)
            {
                projectsQuery = projectsQuery.Where(_ => false);
            }
            else if (effectiveMinistryCode is int scopedOrFiltered)
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.MinistryCode == scopedOrFiltered ||
                    p.Ministries.Any(m => m.Code == scopedOrFiltered));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                projectsQuery = projectsQuery.Where(p => p.ProjectName.Contains(term));
            }

            // Build the ministry filter dropdown. Non-admins are scoped to their own ministry.
            var ministriesQuery = _context.Ministries.AsQueryable();
            if (!isAdmin)
            {
                ministriesQuery = scopedMinistryCode is null
                    ? ministriesQuery.Where(_ => false)
                    : ministriesQuery.Where(m => m.Code == scopedMinistryCode);
            }

            ViewBag.Ministries = await ministriesQuery
                .OrderBy(m => m.MinistryDisplayName_EN)
                .ToListAsync();
            ViewBag.SelectedMinistryCode = ministryCode;
            ViewBag.MinistryCode = ministryCode;
            ViewBag.SearchTerm = search;

            List<Project> projects = await projectsQuery.Distinct().ToListAsync();

            return View(projects);
        }

        public async Task<IActionResult> Phase(int? projectId, int? frameworkCode, int? outcomeCode, int? outputCode, int? subOutputCode, int? indicatorCode, int? ministryCode)
        {
            var phasesQuery = _context.ProjectPhases
                .Include(pp => pp.Project)
                .AsQueryable();

            if (projectId.HasValue)
            {
                phasesQuery = phasesQuery.Where(pp => pp.ProjectID == projectId.Value);
            }
            else if (indicatorCode.HasValue)
            {
                phasesQuery = phasesQuery.Where(pp => pp.Project.Indicators.Any(i => i.IndicatorCode == indicatorCode.Value));
            }
            else if (subOutputCode.HasValue)
            {
                phasesQuery = phasesQuery.Where(pp => pp.Project.Indicators.Any(i => i.SubOutputCode == subOutputCode.Value));
            }
            else if (outputCode.HasValue)
            {
                phasesQuery = phasesQuery.Where(pp => pp.Project.Indicators.Any(i => i.SubOutput.OutputCode == outputCode.Value));
            }
            else if (outcomeCode.HasValue)
            {
                phasesQuery = phasesQuery.Where(pp => pp.Project.Indicators.Any(i => i.SubOutput.Output.OutcomeCode == outcomeCode.Value));
            }
            else if (frameworkCode.HasValue)
            {
                phasesQuery = phasesQuery.Where(pp => pp.Project.Indicators.Any(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode.Value));
            }

            // Same combined scope+filter, and the same either-edge rule as Projects, so the phase
            // count on a ministry card equals the rows this page lists.
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var effectiveMinistryCode = isAdmin ? ministryCode : scopedMinistryCode;

            if (!isAdmin && scopedMinistryCode is null)
            {
                phasesQuery = phasesQuery.Where(_ => false);
            }
            else if (effectiveMinistryCode is int scopedOrFiltered)
            {
                phasesQuery = phasesQuery.Where(pp =>
                    pp.Project.MinistryCode == scopedOrFiltered ||
                    pp.Project.Ministries.Any(m => m.Code == scopedOrFiltered));
            }

            ViewBag.MinistryCode = ministryCode;

            var phases = await phasesQuery.OrderBy(pp => pp.Project.ProjectName).ThenBy(pp => pp.StartDate).ToListAsync();
            return View(phases);
        }

        public async Task<IActionResult> Indicators(
           int? frameworkCode,
           int? outcomeCode,
           int? outputCode,
           int? subOutputCode,
           int? ministryCode)
        {
            var indicatorsQuery = _context.Indicators.AsQueryable();

            // Eager load necessary navigation properties for filtering and project counting
            indicatorsQuery = indicatorsQuery
                .Include(i => i.SubOutput) // Include SubOutput for filtering by Output/Outcome/Framework
                    .ThenInclude(so => so.Output) // Include Output
                        .ThenInclude(o => o.Outcome) // Include Outcome
                            .ThenInclude(outc => outc.Framework) // Include Framework
                .Include(i => i.Project)
                    .ThenInclude(p => p.Phases)
                ;

            if (subOutputCode.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.SubOutputCode == subOutputCode.Value);
            }
            else if (outputCode.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.SubOutput.OutputCode == outputCode.Value);
            }
            else if (outcomeCode.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.SubOutput.Output.OutcomeCode == outcomeCode.Value);
            }
            else if (frameworkCode.HasValue)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode.Value);
            }

            // Ministry scope and the ministry FILTER are one predicate: the query-string value is
            // honoured only for an admin, so a scoped user's ministryCode argument is discarded
            // rather than combined -- widening by hand-editing the URL is impossible by construction.
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            var effectiveMinistryCode = isAdmin ? ministryCode : scopedMinistryCode;

            if (!isAdmin && scopedMinistryCode is null)
            {
                indicatorsQuery = indicatorsQuery.Where(_ => false);
            }
            else if (effectiveMinistryCode is int scopedOrFiltered)
            {
                indicatorsQuery = indicatorsQuery.Where(i => i.SubOutput.Output.Outcome.Framework.MinistryCode == scopedOrFiltered);
            }

            ViewBag.MinistryCode = ministryCode;

            var indicators = await indicatorsQuery.ToListAsync();

            // Create a dictionary of IndicatorCode -> ProjectCount
            // Each indicator has at most 1 project via ProjectID (nullable FK)
            var projectCounts = indicators.ToDictionary(
                i => i.IndicatorCode,
                i => i.ProjectID != null ? 1 : 0
            );

            // Dictionary: IndicatorCode -> Phase Count
            var phaseCounts = indicators.ToDictionary(
                i => i.IndicatorCode,
                i => i.Project?.Phases.Select(ph => ph.Id).Distinct().Count() ?? 0
            );

            ViewBag.ProjectCounts = projectCounts;
            ViewBag.PhaseCounts = phaseCounts;

            return View(indicators);
        }
    }
}
