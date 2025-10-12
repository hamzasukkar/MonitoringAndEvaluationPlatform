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

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class DonorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Donors
        public async Task<IActionResult> Index()

        {

            return View(await _context.Donors.ToListAsync());

        }

        // GET: Donors
        public async Task<IActionResult> ResultIndex(DonorCategory donorCategory)
        {
            var donors = _context.Donors.Where(d => d.donorCategory == donorCategory);

            return View(await donors.ToListAsync());
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
                        .ThenInclude(p => p.Ministries)
                .Include(d => d.ProjectDonors)
                    .ThenInclude(pd => pd.Project)
                        .ThenInclude(p => p.Governorates)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (donor == null)
            {
                return NotFound();
            }

            // Get projects list
            var projects = donor.ProjectDonors.Select(pd => pd.Project).ToList();

            // Calculate statistics
            ViewBag.TotalProjects = projects.Count;
            ViewBag.ActiveProjects = projects.Count(p => p.EndDate >= DateTime.Now);
            ViewBag.CompletedProjects = projects.Count(p => p.EndDate < DateTime.Now);
            ViewBag.TotalBudget = projects.Sum(p => p.EstimatedBudget);
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
        public async Task<IActionResult> InlineDelete(int id)
        {
            var donor = await _context.Donors.FindAsync(id);
            if (donor == null)
                return Json(new { success = false, message = "Donor not found" });

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

            var projects = donor.ProjectDonors.Select(pd => new
            {
                code = pd.Project.ProjectID,
                name = pd.Project.ProjectName,
                location = pd.Project.Governorates.FirstOrDefault() != null ? pd.Project.Governorates.First().AR_Name : "N/A",
                indicatorsPerformance = Math.Round(pd.Project.performance, 2),
                disbursementPerformance = Math.Round(pd.Project.DisbursementPerformance, 2)
            }).ToList();

            return Json(new { success = true, donorName = donor.Partner, projects });
        }
    }
}
