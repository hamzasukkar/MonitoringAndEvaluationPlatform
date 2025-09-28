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
            // 1) Load all ministries (for the filter dropdown, etc.)
            var allMinistries = _context.Ministries.ToList();

            // 2) Start from Frameworks, but now eagerly include Project.Ministries
            var frameworks = _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(o => o.Outputs)
                        .ThenInclude(outp => outp.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.ProjectIndicators)
                                    .ThenInclude(pi => pi.Project)
                                        .ThenInclude(p => p.Ministries)
                .AsQueryable();

            if (selectedMinistryIds != null && selectedMinistryIds.Any())
            {
                frameworks = frameworks
                    .Where(f =>
                        f.Outcomes
                         .SelectMany(o => o.Outputs)
                         .SelectMany(outp => outp.SubOutputs)
                         .SelectMany(so => so.Indicators)
                         .SelectMany(i => i.ProjectIndicators)
                         .Any(pi =>
                             pi.Project.Ministries.Any(min => selectedMinistryIds.Contains(min.Code))
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

            var frameworks = await _context.Frameworks
                .Include(i => i.Outcomes)
                .ThenInclude(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .ThenInclude(i => i.ProjectIndicators)
                .ThenInclude(pi => pi.Project)
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
                .ThenInclude(i => i.ProjectIndicators)
                .ThenInclude(pi => pi.Project)
                .OrderByDescending(f => f.IndicatorsPerformance)
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
            // Start with a query on ProjectIndicators to get projects based on indicators
            IQueryable<ProjectIndicator> projectIndicatorsQuery = _context.ProjectIndicators
                .Include(pi => pi.Project) // Eager load the Project
                .Include(pi => pi.Indicator) // Eager load the Indicator
                    .ThenInclude(i => i.SubOutput) // Eager load SubOutput from Indicator
                        .ThenInclude(so => so.Output) // Eager load Output from SubOutput
                            .ThenInclude(o => o.Outcome) // Eager load Outcome from Output
                                .ThenInclude(outc => outc.Framework); // Eager load Framework from Outcome

            if (indicatorCode.HasValue)
            {
                // Filter by IndicatorCode directly
                projectIndicatorsQuery = projectIndicatorsQuery.Where(pi => pi.IndicatorCode == indicatorCode.Value);
            }
            else if (subOutputCode.HasValue)
            {
                // Filter by SubOutputCode, traversing from ProjectIndicator -> Indicator -> SubOutput
                projectIndicatorsQuery = projectIndicatorsQuery.Where(pi => pi.Indicator.SubOutputCode == subOutputCode.Value);
            }
            else if (outputCode.HasValue)
            {
                // Filter by OutputCode, traversing from ProjectIndicator -> Indicator -> SubOutput -> Output
                projectIndicatorsQuery = projectIndicatorsQuery.Where(pi => pi.Indicator.SubOutput.OutputCode == outputCode.Value);
            }
            else if (outcomeCode.HasValue)
            {
                // Filter by OutcomeCode, traversing from ProjectIndicator -> Indicator -> SubOutput -> Output -> Outcome
                projectIndicatorsQuery = projectIndicatorsQuery.Where(pi => pi.Indicator.SubOutput.Output.OutcomeCode == outcomeCode.Value);
            }
            else if (frameworkCode.HasValue)
            {
                // Filter by FrameworkCode, traversing from ProjectIndicator -> Indicator -> SubOutput -> Output -> Outcome -> Framework
                projectIndicatorsQuery = projectIndicatorsQuery.Where(pi => pi.Indicator.SubOutput.Output.Outcome.FrameworkCode == frameworkCode.Value);
            }
            else
            {
                // If no specific hierarchy level is provided, return all projects that have indicators
            }

            // Select distinct projects from the filtered ProjectIndicators
            List<Project> projects = await projectIndicatorsQuery
                .Select(pi => pi.Project) // Select the Project entity from each ProjectIndicator
                .Distinct() // Get only unique Project entities
                .ToListAsync();

            return View(projects);
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
                .Include(i => i.Measures); // <--- CRUCIAL: Include Measures to count projects via Measures

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

            var indicators = await indicatorsQuery.ToListAsync();

            // Create a dictionary of IndicatorCode -> ProjectCount
            // Now correctly counting distinct projects via the ProjectIndicators collection
            var projectCounts = indicators.ToDictionary(
                i => i.IndicatorCode, // Use i.Code as the key for the dictionary
                i => i.ProjectIndicators // Access the ProjectIndicators collection on the indicator
                      .Select(pi => pi.ProjectId) // Select the ProjectId from each ProjectIndicator
                      .Distinct() // Get distinct ProjectIds
                      .Count() // Count them
            );

            ViewBag.ProjectCounts = projectCounts;

            return View(indicators);
        }
    }
}
