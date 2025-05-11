using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class MonitoringController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MonitoringController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult FrameworkDashboard(List<int> selectedMinistryIds)
        {
            var allMinistries = _context.Ministries.ToList();

            var frameworks = _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(out2 => out2.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.Measures)
                                    .ThenInclude(m => m.Project)
                .AsQueryable();

            if (selectedMinistryIds != null && selectedMinistryIds.Any())
            {
                frameworks = frameworks
                    .Where(f => f.Outcomes
                        .SelectMany(o => o.Outputs)
                        .SelectMany(outp => outp.SubOutputs)
                        .SelectMany(so => so.Indicators)
                        .SelectMany(i => i.Measures)
                        .Any(m => selectedMinistryIds.Contains(m.Project.MinistryCode)));
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

            var frameworks = await _context.Frameworks
                .Include(i => i.Outcomes)
                .ThenInclude(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .ThenInclude(i => i.Measures)
                .ThenInclude(i => i.Project)
                .ToListAsync();
            return View(frameworks);
        }

        // GET: Monitoring
        public async Task<IActionResult> Index(int? frameworkCode)
        {
            var frameworks = await _context.Frameworks
                .Include(i => i.Outcomes)
                .ThenInclude(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .ThenInclude(i => i.Measures)
                .ThenInclude(i => i.Project)
                .ToListAsync();
            return View(frameworks);
        }

        public async Task<IActionResult> Outcome(int? frameworkCode)
        {
            var outcomesQuery = _context.Outcomes
                .Include(o => o.Outputs)
                    .ThenInclude(ou => ou.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                            .ThenInclude(i => i.ProjectIndicators)
                .AsQueryable();

            if (frameworkCode.HasValue)
            {
                outcomesQuery = outcomesQuery.Where(o => o.FrameworkCode == frameworkCode.Value);
            }

            var outcomes = await outcomesQuery.ToListAsync();

            // Dictionary: OutcomeCode -> Distinct Project Count
            var projectCounts = outcomes.ToDictionary(
                o => o.Code,
                o => o.Outputs
                        .SelectMany(ou => ou.SubOutputs)
                        .SelectMany(so => so.Indicators)
                        .SelectMany(i => i.ProjectIndicators)
                        .Select(pi => pi.ProjectId)
                        .Distinct()
                        .Count()
            );

            ViewBag.ProjectCounts = projectCounts;

            return View(outcomes);
        }


        public async Task<IActionResult> Outputs(int? frameworkCode, int? outcomeCode)
        {
            var outputsQuery = _context.Outputs
                .Include(o => o.SubOutputs)
                    .ThenInclude(so => so.Indicators)
                        .ThenInclude(i => i.ProjectIndicators)
                .Include(o => o.Outcome)
                .AsQueryable();

            if (frameworkCode.HasValue)
            {
                outputsQuery = outputsQuery.Where(o => o.Outcome.FrameworkCode == frameworkCode.Value);
            }

            if (outcomeCode.HasValue)
            {
                outputsQuery = outputsQuery.Where(o => o.OutcomeCode == outcomeCode.Value);
            }

            var outputs = await outputsQuery.ToListAsync();

            // Dictionary: OutputCode -> Distinct Project Count
            var projectCounts = outputs.ToDictionary(
                o => o.Code,
                o => o.SubOutputs
                        .SelectMany(so => so.Indicators)
                        .SelectMany(i => i.ProjectIndicators)
                        .Select(pi => pi.ProjectId)
                        .Distinct()
                        .Count()
            );

            ViewBag.ProjectCounts = projectCounts;

            return View(outputs);
        }


        public async Task<IActionResult> SubOutputs(int? frameworkCode, int? outcomeCode, int? outputCode)
        {
            var subOutputsQuery = _context.SubOutputs
                .Include(so => so.Indicators)
                    .ThenInclude(i => i.ProjectIndicators)
                .Include(so => so.Output)
                    .ThenInclude(o => o.Outcome)
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

            var subOutputs = await subOutputsQuery.ToListAsync();

            // Dictionary: SubOutputCode -> Distinct Project Count
            var projectCounts = subOutputs.ToDictionary(
                so => so.Code,
                so => so.Indicators
                        .SelectMany(i => i.ProjectIndicators)
                        .Select(pi => pi.ProjectId)
                        .Distinct()
                        .Count()
            );

            ViewBag.ProjectCounts = projectCounts;

            return View(subOutputs);
        }



        public async Task<IActionResult> Projects(
             int? frameworkCode,
             int? outcomeCode,
             int? outputCode,
             int? subOutputCode,
             int? indicatorCode)
        {
            List<Project> projects = new();

            if (indicatorCode.HasValue)
            {
                projects = await _context.ProjectIndicators
                    .Where(pi => pi.IndicatorCode == indicatorCode.Value)
                    .Select(pi => pi.Project)
                    .Distinct()
                    .ToListAsync();
            }
            else if (subOutputCode.HasValue)
            {
                projects = await _context.ProjectIndicators
                    .Where(pi => pi.Indicator.SubOutputCode == subOutputCode.Value)
                    .Select(pi => pi.Project)
                    .Distinct()
                    .ToListAsync();
            }
            else if (outputCode.HasValue)
            {
                projects = await _context.ProjectIndicators
                    .Where(pi => pi.Indicator.SubOutput.OutputCode == outputCode.Value)
                    .Select(pi => pi.Project)
                    .Distinct()
                    .ToListAsync();
            }
            else if (outcomeCode.HasValue)
            {
                projects = await _context.ProjectIndicators
                    .Where(pi => pi.Indicator.SubOutput.Output.OutcomeCode == outcomeCode.Value)
                    .Select(pi => pi.Project)
                    .Distinct()
                    .ToListAsync();
            }
            else if (frameworkCode.HasValue)
            {
                projects = await _context.ProjectIndicators
                    .Where(pi => pi.Indicator.SubOutput.Output.Outcome.FrameworkCode == frameworkCode.Value)
                    .Select(pi => pi.Project)
                    .Distinct()
                    .ToListAsync();
            }

            return View(projects);
        }

        public async Task<IActionResult> Indicators(
             int? frameworkCode,
             int? outcomeCode,
             int? outputCode,
             int? subOutputCode)
        {
            var indicatorsQuery = _context.Indicators.AsQueryable();

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

            var indicators = await indicatorsQuery.Include(i => i.ProjectIndicators).ToListAsync();

            // Create a dictionary of IndicatorCode -> ProjectCount
            var projectCounts = indicators.ToDictionary(
                i => i.IndicatorCode,
                i => i.ProjectIndicators
                      .Select(pi => pi.ProjectId)
                      .Distinct()
                      .Count()
            );

            ViewBag.ProjectCounts = projectCounts;

            return View(indicators);
        }
    }
}
