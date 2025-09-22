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

        public async Task<IActionResult> Index2(int id)
        {
            ViewData["FrameworkCode"] = id;
            return View();
        }

        public IActionResult GetFrameworkHierarchy(int id)
        {
            var data = _context.Frameworks
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
                weight = 1.0,
                IndicatorsPerformance =Math.Round(f.IndicatorsPerformance,0).ToString()+"%"
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
                weight = o.Weight,
                IndicatorsPerformance = Math.Round(o.IndicatorsPerformance,0).ToString()+"%"
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
                weight = op.Weight,
                IndicatorsPerformance = Math.Round(op.IndicatorsPerformance, 0).ToString()+"%"
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
                weight = so.Weight,
                IndicatorsPerformance = Math.Round(so.IndicatorsPerformance, 0).ToString()+"%"
            }
                }
                .Concat(so.Indicators.Select(i => new
                {
                    id = $"I{i.IndicatorCode}",
                    pid = $"S{so.Code}",
                    name = i.Name,
                    type = "Indicator",
                    weight = i.Weight,
                    IndicatorsPerformance = Math.Round(i.IndicatorsPerformance, 0).ToString() + "%"
                })))))))));

            return Json(data);
        }




    }
}
