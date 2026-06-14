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
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class MonitoringController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MonitoringController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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


        public async Task<IActionResult> Ministry(int? id)
        {
            ViewBag.MinistryList = _context.Ministries.Distinct().ToList();

            var query = _context.Frameworks
                .Include(i => i.Outcomes)
                .ThenInclude(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .ThenInclude(i => i.Project)
                .AsQueryable();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(f => f.MinistryCode == scopedMinistryCode);
            }

            var frameworks = await query.ToListAsync();
            return View(frameworks);
        }

        // GET: Monitoring
        public async Task<IActionResult> Index(int? frameworkCode)
        {
            var query = _context.Frameworks
                .Include(i => i.Outcomes)
                .ThenInclude(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .ThenInclude(i => i.Project)
                    .ThenInclude(p => p.Phases)
                .AsQueryable();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(f => f.MinistryCode == scopedMinistryCode);
            }

            var frameworks = await query.OrderByDescending(f => f.IndicatorsPerformance).ToListAsync();
            return View(frameworks);
        }

        public async Task<IActionResult> Outcome(int? frameworkCode)
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

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                outcomesQuery = scopedMinistryCode is null
                    ? outcomesQuery.Where(_ => false)
                    : outcomesQuery.Where(o => o.Framework.MinistryCode == scopedMinistryCode);
            }

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


        public async Task<IActionResult> Outputs(int? frameworkCode, int? outcomeCode)
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

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                outputsQuery = scopedMinistryCode is null
                    ? outputsQuery.Where(_ => false)
                    : outputsQuery.Where(o => o.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

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


        public async Task<IActionResult> SubOutputs(int? frameworkCode, int? outcomeCode, int? outputCode)
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

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                subOutputsQuery = scopedMinistryCode is null
                    ? subOutputsQuery.Where(_ => false)
                    : subOutputsQuery.Where(so => so.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

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
            int? ministryCode)
        {
            // Query projects linked via Indicator.ProjectID
            var projectsQuery = _context.Projects
                .Include(p => p.Sectors)
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

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                projectsQuery = scopedMinistryCode is null
                    ? projectsQuery.Where(_ => false)
                    : projectsQuery.Where(p => p.MinistryCode == scopedMinistryCode);
            }

            if (ministryCode.HasValue)
            {
                projectsQuery = projectsQuery.Where(p => p.Ministries.Any(m => m.Code == ministryCode.Value));
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

            List<Project> projects = await projectsQuery.Distinct().ToListAsync();

            return View(projects);
        }

        public async Task<IActionResult> Phase(int? projectId, int? frameworkCode, int? outcomeCode, int? outputCode, int? subOutputCode, int? indicatorCode)
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

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                phasesQuery = scopedMinistryCode is null
                    ? phasesQuery.Where(_ => false)
                    : phasesQuery.Where(pp => pp.Project.MinistryCode == scopedMinistryCode);
            }

            var phases = await phasesQuery.OrderBy(pp => pp.Project.ProjectName).ThenBy(pp => pp.StartDate).ToListAsync();
            return View(phases);
        }

        public async Task<IActionResult> Indicators(
           int? frameworkCode,
           int? outcomeCode,
           int? outputCode,
           int? subOutputCode)
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

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                indicatorsQuery = scopedMinistryCode is null
                    ? indicatorsQuery.Where(_ => false)
                    : indicatorsQuery.Where(i => i.SubOutput.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

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
