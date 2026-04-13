using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using System.Security.Claims;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class DebugController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DebugController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult UserInfo()
        {
            var user = HttpContext.User;

            ViewBag.IsAuthenticated = user.Identity?.IsAuthenticated;
            ViewBag.UserName = user.Identity?.Name;
            ViewBag.Claims = user.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
            ViewBag.Roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return View();
        }

        /// <summary>
        /// Seeds a complete demo hierarchy with 12 months of performance data so the
        /// Indicator trend chart can be tested immediately.
        /// Visit: /Debug/SeedTrendDemo
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SeedTrendDemo()
        {
            // Reuse existing demo data if already seeded
            var existing = await _context.Indicators
                .FirstOrDefaultAsync(i => i.Name == "[DEMO] Test Indicator – Trend Chart");

            if (existing != null)
                return Redirect($"/Indicators/Details/{existing.IndicatorCode}");

            // ── 1. Ensure a ProjectManager and Supervisor exist ──────────────
            var pm = await _context.ProjectManagers.FirstOrDefaultAsync()
                ?? new ProjectManager { Name = "Demo PM", PhoneNumber = "0000000", Email = "pm@demo.com" };
            if (pm.Code == 0) { _context.ProjectManagers.Add(pm); await _context.SaveChangesAsync(); }

            var sv = await _context.SuperVisors.FirstOrDefaultAsync()
                ?? new SuperVisor { Name = "Demo Supervisor", PhoneNumber = "0000000", Email = "sv@demo.com" };
            if (sv.Code == 0) { _context.SuperVisors.Add(sv); await _context.SaveChangesAsync(); }

            // ── 2. Framework → Outcome → Output → SubOutput ──────────────────
            var framework = new Framework { Name = "[DEMO] Framework" };
            _context.Frameworks.Add(framework);
            await _context.SaveChangesAsync();

            var outcome = new Outcome { Name = "[DEMO] Outcome", FrameworkCode = framework.Code };
            _context.Outcomes.Add(outcome);
            await _context.SaveChangesAsync();

            var output = new Output { Name = "[DEMO] Output", OutcomeCode = outcome.Code };
            _context.Outputs.Add(output);
            await _context.SaveChangesAsync();

            var subOutput = new SubOutput { Name = "[DEMO] Sub-Output", OutputCode = output.Code };
            _context.SubOutputs.Add(subOutput);
            await _context.SaveChangesAsync();

            // ── 3. Indicator ─────────────────────────────────────────────────
            var indicator = new Indicator
            {
                Name = "[DEMO] Test Indicator – Trend Chart",
                SubOutputCode = subOutput.Code,
                Target = 100,
                Weight = 1,
                TargetYear = DateTime.Today.AddYears(1),
                Source = "Demo",
                Concept = "Demo indicator for testing the performance trend chart."
            };
            _context.Indicators.Add(indicator);
            await _context.SaveChangesAsync();

            // ── 4. Project ───────────────────────────────────────────────────
            var project = new Project
            {
                ProjectName = "[DEMO] Test Project",
                EstimatedBudget = 1_000_000,
                Currency = "USD",
                StartDate = DateTime.Today.AddMonths(-12),
                EndDate = DateTime.Today.AddMonths(12),
                ProjectManagerCode = pm.Code,
                SuperVisorCode = sv.Code,
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Link indicator → project
            indicator.ProjectID = project.ProjectID;
            await _context.SaveChangesAsync();

            // ── 5. Two project phases ─────────────────────────────────────────
            var phase1 = new ProjectPhase { Name = "Phase 1 – Planning", ProjectID = project.ProjectID, Weight = 50 };
            var phase2 = new ProjectPhase { Name = "Phase 2 – Execution", ProjectID = project.ProjectID, Weight = 50 };
            _context.ProjectPhases.AddRange(phase1, phase2);
            await _context.SaveChangesAsync();

            // ── 6. Measures: 12 months of data, two phases ───────────────────
            // Phase 1: rising trend (months -12 to -7)
            double[] phase1Values = { 30, 38, 45, 52, 60, 67 };
            // Phase 2: dip then recovery (months -6 to -1)
            double[] phase2Values = { 55, 48, 62, 70, 78, 85 };

            var measures = new List<Measure>();
            for (int i = 0; i < 6; i++)
            {
                measures.Add(new Measure
                {
                    Name = $"Phase 1 – Month {i + 1}",
                    Date = DateTime.Today.AddMonths(-12 + i),
                    Value = phase1Values[i],
                    MeasureType = MonitoringAndEvaluationPlatform.Enums.MeasureType.Qualitative,
                    ProjectPhaseId = phase1.Id
                });
                measures.Add(new Measure
                {
                    Name = $"Phase 2 – Month {i + 1}",
                    Date = DateTime.Today.AddMonths(-6 + i),
                    Value = phase2Values[i],
                    MeasureType = MonitoringAndEvaluationPlatform.Enums.MeasureType.Qualitative,
                    ProjectPhaseId = phase2.Id
                });
            }

            _context.Measures.AddRange(measures);
            await _context.SaveChangesAsync();

            return Redirect($"/Indicators/Details/{indicator.IndicatorCode}");
        }

        /// <summary>Removes all [DEMO] data created by SeedTrendDemo.</summary>
        [HttpGet]
        public async Task<IActionResult> CleanTrendDemo()
        {
            var indicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.Name == "[DEMO] Test Indicator – Trend Chart");

            if (indicator != null)
            {
                if (indicator.ProjectID.HasValue)
                {
                    var phases = await _context.ProjectPhases
                        .Where(p => p.ProjectID == indicator.ProjectID.Value).ToListAsync();
                    var phaseIds = phases.Select(p => p.Id).ToList();
                    var meas = await _context.Measures
                        .Where(m => phaseIds.Contains(m.ProjectPhaseId)).ToListAsync();
                    _context.Measures.RemoveRange(meas);
                    _context.ProjectPhases.RemoveRange(phases);
                    var proj = await _context.Projects.FindAsync(indicator.ProjectID.Value);
                    if (proj != null) _context.Projects.Remove(proj);
                }
                _context.Indicators.Remove(indicator);
            }

            foreach (var n in new[] { "[DEMO] Sub-Output", "[DEMO] Output", "[DEMO] Outcome", "[DEMO] Framework" })
            {
                var so = await _context.SubOutputs.FirstOrDefaultAsync(x => x.Name == n);
                if (so != null) _context.SubOutputs.Remove(so);
                var op = await _context.Outputs.FirstOrDefaultAsync(x => x.Name == n);
                if (op != null) _context.Outputs.Remove(op);
                var oc = await _context.Outcomes.FirstOrDefaultAsync(x => x.Name == n);
                if (oc != null) _context.Outcomes.Remove(oc);
                var fw = await _context.Frameworks.FirstOrDefaultAsync(x => x.Name == n);
                if (fw != null) _context.Frameworks.Remove(fw);
            }

            await _context.SaveChangesAsync();
            return Content("Demo data cleaned up successfully.");
        }
    }
}