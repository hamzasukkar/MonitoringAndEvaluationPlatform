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
        public async Task<IActionResult> Index(int id)
        {
            ViewData["FrameworkCode"] = id;
            return View();
        }

        public IActionResult GetFrameworkHierarchy(int id)
        {
            var data = _context.Framework
                .Include(f => f.Outcomes)
                .ThenInclude(o => o.Outputs)
                .ThenInclude(op => op.SubOutputs)
                .ThenInclude(so => so.Indicators)
                .Where(i=>i.Code== id)
                .ToList()
                .SelectMany(f => new[]
                {
            new
            {
                id = $"F{f.Code}",
                pid = "",
                name = f.Name,
                type = "Framework",
                IndicatorsPerformance = f.IndicatorsPerformance.ToString()+"%"
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
                IndicatorsPerformance = o.IndicatorsPerformance.ToString()+"%"
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
                IndicatorsPerformance = op.IndicatorsPerformance.ToString()+"%"
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
                IndicatorsPerformance = so.IndicatorsPerformance.ToString()+"%"
            }
                }
                .Concat(so.Indicators.Select(i => new
                {
                    id = $"I{i.IndicatorCode}",
                    pid = $"S{so.Code}",
                    name = i.Name,
                    type = "Indicator",
                    IndicatorsPerformance = i.IndicatorsPerformance.ToString() + "%"
                })))))))));

            return Json(data);
        }




    }
}
