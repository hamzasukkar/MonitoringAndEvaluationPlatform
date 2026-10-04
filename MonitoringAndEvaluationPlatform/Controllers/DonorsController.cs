using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using Microsoft.Extensions.Localization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    // Login required: nothing here is public. No fallback policy exists, so without this
    // attribute every action was reachable anonymously.
    [Authorize]
    public class DonorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<DonorsController> _localizer;
        private readonly ICurrencyConversionService _currencyConversion;

        private readonly IMinistryScopeService _ministryScope;

        public DonorsController(ApplicationDbContext context, IStringLocalizer<DonorsController> localizer, ICurrencyConversionService currencyConversion, IMinistryScopeService ministryScope)
        {
            _currencyConversion = currencyConversion;
            _context = context;
            _localizer = localizer;
            _ministryScope = ministryScope;
        }

        /// <summary>
        /// A donor's stored performance is a national figure built from every ministry's projects.
        /// An administrator gets it as stored; anyone else gets only the donors their own projects
        /// involve, with the figures recomputed from those projects. Loaded AsNoTracking so the
        /// recomputed values can never be saved onto the shared rows.
        /// </summary>
        private async Task<List<Donor>> ScopedDonorsAsync(IQueryable<Donor> donors)
        {
            var scope = await _ministryScope.GetScopeAsync();
            if (scope.IsAdmin) return await donors.ToListAsync();

            var list = await donors.AsNoTracking().ToListAsync();
            var ownProjects = await _context.Projects
                .AsNoTracking()
                .WithinScope(scope)
                .Include(p => p.Donors)
                .Include(p => p.Phases)
                    .ThenInclude(ph => ph.ActionPlan)
                        .ThenInclude(ap => ap!.Plans)
                .ToListAsync();
            var converter = await _currencyConversion.GetConverterAsync();

            var result = new List<Donor>();
            foreach (var donor in list)
            {
                var donorProjects = ownProjects.Where(p => p.Donors.Any(d => d.Code == donor.Code)).ToList();
                if (donorProjects.Count == 0) continue;

                donor.IndicatorsPerformance = ScopedAggregates.IndicatorsPerformance(donorProjects);
                donor.DisbursementPerformance = ScopedAggregates.DisbursementPerformance(donorProjects, converter);
                result.Add(donor);
            }
            return result;
        }

        // GET: Donors
        public async Task<IActionResult> Index()
        {
            // Get donors sorted by IndicatorsPerformance in descending order (large to small)
            var donors = (await ScopedDonorsAsync(_context.Donors))
                .OrderByDescending(d => d.IndicatorsPerformance)
                .ToList();

            return View(donors);
        }

        // GET: Donors
        public async Task<IActionResult> ResultIndex(DonorCategory donorCategory)
        {
            var donors = _context.Donors.Where(d => d.donorCategory == donorCategory);
            ViewData["DonorCategory"] = donorCategory;
            return View(await ScopedDonorsAsync(donors));
        }

        // GET: Donors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donor = await _context.Donors
                .Include(d => d.ProjectDonors)
                    .ThenInclude(pd => pd.Project)
                        .ThenInclude(p => p.ProjectManager)
                .Include(d => d.ProjectDonors)
                    .ThenInclude(pd => pd.Project)
                        .ThenInclude(p => p.SuperVisor)
                .Include(d => d.ProjectDonors)
                    .ThenInclude(pd => pd.Project)
                        .ThenInclude(p => p.Ministry)
                .Include(d => d.ProjectDonors)
                    .ThenInclude(pd => pd.Project)
                        .ThenInclude(p => p.Governorates)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (donor == null)
            {
                return NotFound();
            }

            // Get projects list: only the caller's own; a donor funds several ministries' projects.
            var scope = await _ministryScope.GetScopeAsync();
            var projects = donor.ProjectDonors
                .Select(pd => pd.Project)
                .Where(p => scope.CanSee(p.MinistryCode))
                .ToList();

            // Calculate statistics
            ViewBag.TotalProjects = projects.Count;
            ViewBag.ActiveProjects = projects.Count(p => p.EndDate >= DateTime.Now);
            ViewBag.CompletedProjects = projects.Count(p => p.EndDate < DateTime.Now);
            ViewBag.TotalBudget = (await _currencyConversion.GetConverterAsync()).SumBudget(projects);
            ViewBag.Projects = projects;

            return View(donor);
        }

        private bool DonorExists(int id)
        {
            return _context.Donors.Any(e => e.Code == id);
        }

        // Inline Operations
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        public async Task<IActionResult> CreateInline(string Partner, int donorCategory)
        {
            if (string.IsNullOrWhiteSpace(Partner))
            {
                return Json(new { success = false, message = "Partner name is required." });
            }

            var donor = new Donor
            {
                Partner = Partner,
                donorCategory = (MonitoringAndEvaluationPlatform.Enums.DonorCategory)donorCategory
            };

            try
            {
                _context.Donors.Add(donor);
                await _context.SaveChangesAsync();
                return Json(new { success = true, donor = new { donor.Code, donor.Partner, donorCategory = donor.donorCategory.ToString(), donorCategoryValue = (int)donor.donorCategory } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating donor: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        public async Task<IActionResult> InlineEdit(int id, string field, string value)
        {
            var donor = await _context.Donors.FindAsync(id);
            if (donor == null)
                return Json(new { success = false, message = "Donor not found" });

            switch (field.ToLower())
            {
                case "partner":
                    donor.Partner = value;
                    break;
                case "donorcategory":
                    if (int.TryParse(value, out int categoryValue))
                    {
                        donor.donorCategory = (MonitoringAndEvaluationPlatform.Enums.DonorCategory)categoryValue;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Invalid donor category" });
                    }
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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        public async Task<IActionResult> InlineDelete(int id)
        {
            var donor = await _context.Donors.FindAsync(id);
            if (donor == null)
                return Json(new { success = false, message = "Donor not found" });

            // Both project links cascade: deleting a donor in use would silently wipe its funding
            // amounts and percentages from every project it funds.
            var projectCount = await _context.Projects.CountAsync(p =>
                p.Donors!.Any(d => d.Code == id) || p.ProjectDonors.Any(pd => pd.DonorCode == id));
            if (projectCount > 0)
                return Json(new { success = false, message = _localizer["This donor is linked to {0} project(s), including their funding records. Remove it from those projects before deleting.", projectCount].Value });

            try
            {
                _context.Donors.Remove(donor);
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
        public async Task<IActionResult> QuickUpdate(int id, string partner, int donorCategory)
        {
            var donor = await _context.Donors.FindAsync(id);
            if (donor == null)
                return Json(new { success = false, message = "Donor not found" });

            if (string.IsNullOrWhiteSpace(partner))
                return Json(new { success = false, message = "Partner name is required" });

            donor.Partner = partner;
            donor.donorCategory = (DonorCategory)donorCategory;

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

        // GET: Donors/GetDonorProjects/5
        [HttpGet]
        public async Task<IActionResult> GetDonorProjects(int id)
        {
            var donor = await _context.Donors
                .Include(d => d.ProjectDonors)
                    .ThenInclude(pd => pd.Project)
                        .ThenInclude(p => p.Governorates)
                .FirstOrDefaultAsync(d => d.Code == id);

            if (donor == null)
                return Json(new { success = false, message = "Donor not found" });

            var scope = await _ministryScope.GetScopeAsync();
            var projects = donor.ProjectDonors
                .Where(pd => scope.CanSee(pd.Project.MinistryCode))
                .Select(pd => new
            {
                code = pd.Project.ProjectID,
                name = pd.Project.ProjectName,
                location = pd.Project.Governorates.FirstOrDefault() != null ? pd.Project.Governorates.First().AR_Name : _localizer["unavailable"].Value,
                indicatorsPerformance = Math.Round(pd.Project.performance, 2),
                disbursementPerformance = Math.Round(pd.Project.DisbursementPerformance, 2)
            }).ToList();

            return Json(new { success = true, donorName = donor.Partner, projects });
        }
    }
}
