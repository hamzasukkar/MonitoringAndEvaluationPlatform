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
    public class TreeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TreeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Frameworks1
        public async Task<IActionResult> Index()
        
        {
            return View(await _context.Freamework.ToListAsync());
        }

        public IActionResult GetFrameworkHierarchy()
        {
            var data = _context.Freamework
                .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                .ThenInclude(op => op.SubOutputs)
                .ThenInclude(so => so.Indicators)
                .ToList()
                .SelectMany(f => new[]
                {
            new { id = $"F{f.Code}", pid = "", name = f.Name, type = "Framework" }
                }
                .Concat(f.Outcomes.SelectMany(o => new[]
                {
            new { id = $"O{o.Code}", pid = $"F{f.Code}", name = o.Name, type = "Outcome" }
                }
                .Concat(o.Outputs.SelectMany(op => new[]
                {
            new { id = $"Op{op.Code}", pid = $"O{o.Code}", name = op.Name, type = "Output" }
                }
                .Concat(op.SubOutputs.SelectMany(so => new[]
                {
            new { id = $"S{so.Code}", pid = $"Op{op.Code}", name = so.Name, type = "SubOutput" }
                }
                .Concat(so.Indicators.Select(i => new
                {
                    id = $"I{i.Code}",
                    pid = $"S{so.Code}",
                    name = i.Name,
                    type = "Indicator"
                })))))))));

            return Json(data);
        }



    }
}
