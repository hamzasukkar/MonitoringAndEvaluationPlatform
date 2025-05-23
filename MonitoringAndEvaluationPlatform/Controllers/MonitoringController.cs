﻿using System;
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
        public async Task<IActionResult> Index(int? id)
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

        public async Task<IActionResult> Outcome(int? id)
        {
            var outcomes = await _context.Outcomes
                .Include(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .Where(x => x.FrameworkCode == id).ToListAsync();

            return View(outcomes);
        }
        public async Task<IActionResult> Output(int? id)
        {
            var outputs = await _context.Outputs
                .Include(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .Where(x => x.OutcomeCode == id).ToListAsync();
            return View(outputs);
        }

        public async Task<IActionResult> FrameworkOutputs(int? id)
        {
            var outputs = await _context.Outputs
                .Include(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .Where(x => x.Outcome.FrameworkCode == id).ToListAsync();
            return View(outputs);
        }

        public async Task<IActionResult> SubOutput(int? id)
        {
            var subOutputs = await _context.SubOutputs
                .Include(i => i.Indicators)
                .Where(x => x.OutputCode == id).ToListAsync();
            return View(subOutputs);
        }

        public async Task<IActionResult> FrameworkSubOutputs(int? id)
        {
            var subOutputs = await _context.SubOutputs
                .Include(i => i.Indicators)
                .Where(x => x.Output.Outcome.FrameworkCode == id).ToListAsync();
            return View(subOutputs);
        }

        public async Task<IActionResult> OutcomeSubOutputs(int? id)
        {
            var subOutputs = await _context.SubOutputs
                .Include(i => i.Indicators)
                .Where(x => x.Output.OutcomeCode == id).ToListAsync();
            return View(subOutputs);
        }


        public async Task<IActionResult> OutputSubOutputs(int? id)
        {
            var subOutputs = await _context.SubOutputs
                .Include(i => i.Indicators)
                .Where(x => x.OutputCode == id).ToListAsync();
            return View(subOutputs);
        }

        public async Task<IActionResult> FrameworkIndicators(int? id)
        {
            var indicators = await _context.Indicators
                .Include(i => i.Measures)
                .ThenInclude(i => i.Project)
                .Where(x => x.SubOutput.Output.Outcome.FrameworkCode == id).ToListAsync();
            return View(indicators);
        }

        // Get Projects linked to a specific Framework
        public async Task<IActionResult> FrameworkProjects(int? id)
        {
            var projects = await _context.Projects
                .Where(p => p.Measures.Any(m =>
                    _context.Indicators.Any(i =>
                        i.IndicatorCode == m.IndicatorCode &&
                        i.SubOutput.Output.Outcome.FrameworkCode == id
                    )
                ))
                .Distinct()
                .ToListAsync();

            return View(projects);
        }
        public async Task<IActionResult> OutcomeIndicators(int? id)
        {
            var indicators = await _context.Indicators
                .Where(x => x.SubOutput.Output.OutcomeCode == id).ToListAsync();
            return View(indicators);
        }

        public async Task<IActionResult> OutputIndicators(int? id)
        {
            var indicators = await _context.Indicators
                .Where(x => x.SubOutput.OutputCode == id).ToListAsync();
            return View(indicators);
        }

        public async Task<IActionResult> SubOutputIndicators(int? id)
        {
            var indicators = await _context.Indicators
                .Where(x => x.SubOutputCode == id).ToListAsync();
            return View(indicators);
        }

        public async Task<IActionResult> Indicator(int? id)
        {
            var indicators = await _context.Indicators
                .Where(x => x.SubOutputCode == id).ToListAsync();
            return View(indicators);
        }
    }
}
