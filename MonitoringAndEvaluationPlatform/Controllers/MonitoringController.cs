using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class MonitoringController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MonitoringController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Monitoring
        public async Task<IActionResult> Index(int? id)
        {
            var frameworks = await _context.Frameworks
                .Include(i => i.Outcomes)
                .ThenInclude(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
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
                .Where(x=>x.Output.Outcome.FrameworkCode==id).ToListAsync();
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
                .Where(x => x.SubOutput.Output.Outcome.FrameworkCode == id).ToListAsync();
            return View(indicators);
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
                .Where(x=>x.SubOutputCode==id).ToListAsync();
            return View(indicators);
        }
    }
}
