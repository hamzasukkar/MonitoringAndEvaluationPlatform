using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    // Login required: nothing here is public. No fallback policy exists, so without this
    // attribute every action was reachable anonymously.
    [Authorize]
    public class SectorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly ICurrencyConversionService _currencyConversion;
        private readonly IMinistryScopeService _ministryScope;

        public SectorsController(ApplicationDbContext context, ICurrencyConversionService currencyConversion, IMinistryScopeService ministryScope)
        {
            _currencyConversion = currencyConversion;
            _context = context;
            _ministryScope = ministryScope;
        }

        /// <summary>
        /// A sector's stored performance is a national figure built from every ministry's projects.
        /// An administrator gets it as stored; anyone else gets only the sectors their own projects
        /// fall in, with the figures recomputed from those projects. Loaded AsNoTracking so the
        /// recomputed values can never be saved onto the shared rows.
        /// </summary>
        private async Task<List<Sector>> ScopedSectorsAsync(IQueryable<Sector> sectors)
        {
            var scope = await _ministryScope.GetScopeAsync();
            if (scope.IsAdmin) return await sectors.ToListAsync();

            var list = await sectors.AsNoTracking().ToListAsync();
            var ownProjects = await _context.Projects
                .AsNoTracking()
                .WithinScope(scope)
                .Include(p => p.Phases)
                    .ThenInclude(ph => ph.ActionPlan)
                        .ThenInclude(ap => ap!.Plans)
                .ToListAsync();
            var converter = await _currencyConversion.GetConverterAsync();

            var result = new List<Sector>();
            foreach (var sector in list)
            {
                var sectorProjects = ownProjects.Where(p => p.SectorCode == sector.Code).ToList();
                if (sectorProjects.Count == 0) continue;

                sector.IndicatorsPerformance = ScopedAggregates.IndicatorsPerformance(sectorProjects);
                sector.DisbursementPerformance = ScopedAggregates.DisbursementPerformance(sectorProjects, converter);
                result.Add(sector);
            }
            return result;
        }

        // GET: Sectors
        public async Task<IActionResult> Index()
        {
            // Get sectors sorted by IndicatorsPerformance in descending order (large to small)
            var sectors = (await ScopedSectorsAsync(_context.Sectors))
                .OrderByDescending(s => s.IndicatorsPerformance)
                .ToList();

            return View(sectors);
        }

        public async Task<IActionResult> ResultIndex(int code)
        {
            if (!await _context.Sectors.AnyAsync(s => s.Code == code))
            {
                return NotFound();
            }

            return View(await ScopedSectorsAsync(_context.Sectors.Where(s => s.Code == code)));
        }

        // GET: Sectors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // AsNoTracking: Projects is narrowed below to the caller's own, which must never be
            // saved back.
            var sector = await _context.Sectors
                .AsNoTracking()
                .Include(s => s.Projects)
                    .ThenInclude(p => p.ProjectManager)
                .Include(s => s.Projects)
                    .ThenInclude(p => p.SuperVisor)
                .Include(s => s.Projects)
                    .ThenInclude(p => p.Ministry)
                .Include(s => s.Projects)
                    .ThenInclude(p => p.Governorates)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (sector == null)
            {
                return NotFound();
            }

            // A sector spans every ministry; list only the caller's own projects in it.
            var scope = await _ministryScope.GetScopeAsync();
            sector.Projects = sector.Projects.Where(p => scope.CanSee(p.MinistryCode)).ToList();

            // Calculate statistics
            ViewBag.TotalProjects = sector.Projects.Count;
            ViewBag.ActiveProjects = sector.Projects.Count(p => p.EndDate >= DateTime.Now);
            ViewBag.CompletedProjects = sector.Projects.Count(p => p.EndDate < DateTime.Now);
            ViewBag.TotalBudget = (await _currencyConversion.GetConverterAsync()).SumBudget(sector.Projects);

            return View(sector);
        }

        private bool SectorExists(int id)
        {
            return _context.Sectors.Any(e => e.Code == id);
        }

        // Inline Operations
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        public async Task<IActionResult> CreateInline(string EN_Name, string AR_Name)
        {
            if (string.IsNullOrWhiteSpace(EN_Name) || string.IsNullOrWhiteSpace(AR_Name))
            {
                return Json(new { success = false, message = "English and Arabic names are required." });
            }

            var sector = new Sector
            {
                EN_Name = EN_Name,
                AR_Name = AR_Name
            };

            try
            {
                _context.Sectors.Add(sector);
                await _context.SaveChangesAsync();
                return Json(new { success = true, sector = new { sector.Code, sector.EN_Name, sector.AR_Name } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating sector: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        public async Task<IActionResult> InlineEdit(int id, string field, string value)
        {
            var sector = await _context.Sectors.FindAsync(id);
            if (sector == null)
                return Json(new { success = false, message = "Sector not found" });

            switch (field.ToLower())
            {
                case "en_name":
                    sector.EN_Name = value;
                    break;
                case "ar_name":
                    sector.AR_Name = value;
                    break;
                default:
                    return Json(new { success = false, message = "Invalid field" });
            }

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        public async Task<IActionResult> InlineDelete(int id)
        {
            var sector = await _context.Sectors.FindAsync(id);
            if (sector == null)
                return Json(new { success = false, message = "Sector not found" });

            // The FK is Restrict — surface a friendly message instead of a constraint exception
            var projectsUsingSector = await _context.Projects.CountAsync(p => p.SectorCode == id);
            if (projectsUsingSector > 0)
            {
                return Json(new { success = false, message = $"This sector is in use by {projectsUsingSector} project(s) and cannot be deleted." });
            }

            try
            {
                _context.Sectors.Remove(sector);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        public async Task<IActionResult> QuickUpdate(int id, string enName, string arName)
        {
            var sector = await _context.Sectors.FindAsync(id);
            if (sector == null)
                return Json(new { success = false, message = "Sector not found" });

            if (string.IsNullOrWhiteSpace(enName) || string.IsNullOrWhiteSpace(arName))
                return Json(new { success = false, message = "Both names are required" });

            sector.EN_Name = enName;
            sector.AR_Name = arName;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
