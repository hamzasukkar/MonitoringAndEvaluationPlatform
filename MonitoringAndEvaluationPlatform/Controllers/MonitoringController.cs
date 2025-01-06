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
        public async Task<IActionResult> Index()
        {
            var freameworks = await _context.Freamework
                .Include(i => i.Outcomes)
                .ThenInclude(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .ToListAsync();
            return View(freameworks);
        }

        public async Task<IActionResult> Outcome()
        {
            var outcomes = await _context.Outcomes
                .Include(i => i.Outputs)
                .ThenInclude(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .ToListAsync();
            return View(outcomes);
        }

        public async Task<IActionResult> Output()
        {
            var outputs = await _context.Outputs
                .Include(i => i.SubOutputs)
                .ThenInclude(i => i.Indicators)
                .ToListAsync();
            return View(outputs);
        }
        public async Task<IActionResult> SubOutput()
        {
            var subOutputs = await _context.SubOutputs
                .Include(i => i.Indicators)
                .ToListAsync();
            return View(subOutputs);
        }
    }
}
