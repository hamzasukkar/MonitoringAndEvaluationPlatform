using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class MinistriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public MinistriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        // GET: Ministries
        [Permission(Permissions.ReadMinistries)]
        public async Task<IActionResult> Index()
        {
            var ministries = await _context.Ministries.ToListAsync();
            return View(ministries);
        }

        // GET: Ministries
        public async Task<IActionResult> ResultIndex(int? ministryCode)
        {
            IQueryable<Ministry> query = _context.Ministries
                .Include(m => m.Projects)
                    .ThenInclude(p => p.Sectors)
                .Include(m => m.Projects)
                    .ThenInclude(p => p.Donors)
                .Include(m => m.Projects)
                    .ThenInclude(p => p.ProjectManager)
                .Include(m => m.Projects)
                    .ThenInclude(p => p.SuperVisor);

            if (ministryCode.HasValue)
            {
                // Show only the ministry with the given code
                query = query.Where(m => m.Code == ministryCode.Value);
            }

            var ministries = await query.ToListAsync();

            // Calculate overall statistics
            var allProjects = ministries.SelectMany(m => m.Projects).Distinct().ToList();
            ViewBag.TotalProjects = allProjects.Count;
            ViewBag.ActiveProjects = allProjects.Count(p => p.EndDate >= DateTime.Now);
            ViewBag.CompletedProjects = allProjects.Count(p => p.EndDate < DateTime.Now);
            ViewBag.TotalBudget = allProjects.Sum(p => p.EstimatedBudget);

            return View(ministries);
        }

        // GET: Ministries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ministry = await _context.Ministries
                .FirstOrDefaultAsync(m => m.Code == id);
            if (ministry == null)
            {
                return NotFound();
            }

            // Get associated projects
            var projects = await _context.Projects
                .Include(p => p.Sectors)
                .Include(p => p.Donors)
                .Include(p => p.ProjectManager)
                .Include(p => p.SuperVisor)
                .Where(p => p.Ministries.Any(m => m.Code == id))
                .ToListAsync();

            // Calculate statistics
            ViewBag.TotalProjects = projects.Count;
            ViewBag.ActiveProjects = projects.Count(p => p.EndDate >= DateTime.Now);
            ViewBag.CompletedProjects = projects.Count(p => p.EndDate < DateTime.Now);
            ViewBag.TotalBudget = projects.Sum(p => p.EstimatedBudget);
            ViewBag.Projects = projects;

            // Get ministry users
            var ministryUsers = await _userManager.Users
                .Where(u => u.MinistryName == ministry.MinistryUserName)
                .ToListAsync();
            ViewBag.MinistryUsers = ministryUsers;

            return View(ministry);
        }

        // 🔹 Create Ministry (Automatically Creates User & Role)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ministry ministry)
        {
            if (ModelState.IsValid)
            {
                // 🔹 Add Ministry to Database
                _context.Ministries.Add(ministry);
                await _context.SaveChangesAsync();

                // 🔹 Create Role (if it doesn’t exist)
                if (!await _roleManager.RoleExistsAsync(ministry.MinistryUserName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(ministry.MinistryUserName));
                }

                // 🔹 Create User for the Ministry
                string defaultPassword = "Ministry@123";  // ⚠️ Change in production
                var user = new ApplicationUser
                {
                    UserName = ministry.MinistryUserName,
                    Email = $"{ministry.MinistryUserName.ToLower()}@example.com", // Example email
                    EmailConfirmed = true,
                    MinistryName = ministry.MinistryUserName
                };

                var result = await _userManager.CreateAsync(user, defaultPassword);
                if (result.Succeeded)
                {
                    // 🔹 Assign User to Role
                    await _userManager.AddToRoleAsync(user, ministry.MinistryUserName);
                }
                else
                {
                    // Log errors (in production, use a logging framework)
                    Console.WriteLine($"⚠️ User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                return RedirectToAction(nameof(Index)); // Redirect to list of ministries
            }

            return View(ministry);
        }

        private bool MinistryExists(int id)
        {
            return _context.Ministries.Any(e => e.Code == id);
        }

        // Inline Operations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(string MinistryDisplayName_AR, string MinistryDisplayName_EN, string MinistryUserName, string Logo)
        {
            if (string.IsNullOrWhiteSpace(MinistryDisplayName_AR) && string.IsNullOrWhiteSpace(MinistryDisplayName_EN))
            {
                return Json(new { success = false, message = "Display Name (Arabic or English) is required." });
            }

            var ministry = new Ministry
            {
                MinistryDisplayName_AR = MinistryDisplayName_AR,
                MinistryDisplayName_EN = MinistryDisplayName_EN,
                MinistryUserName = MinistryUserName ?? (MinistryDisplayName_EN ?? MinistryDisplayName_AR).Replace(" ", "").ToLower(),
                Logo = Logo
            };

            try
            {
                _context.Ministries.Add(ministry);
                await _context.SaveChangesAsync();
                return Json(new { success = true, ministry = new { ministry.Code, ministry.MinistryDisplayName_AR, ministry.MinistryDisplayName_EN, ministry.MinistryUserName, ministry.Logo } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating ministry: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InlineEdit(int id, string field, string value)
        {
            var ministry = await _context.Ministries.FindAsync(id);
            if (ministry == null)
                return Json(new { success = false, message = "Ministry not found" });

            switch (field.ToLower())
            {
                case "ministrydisplayname_ar":
                    ministry.MinistryDisplayName_AR = value;
                    break;
                case "ministrydisplayname_en":
                    ministry.MinistryDisplayName_EN = value;
                    break;
                case "ministryusername":
                    ministry.MinistryUserName = value;
                    break;
                case "logo":
                    ministry.Logo = value;
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
        public async Task<IActionResult> QuickUpdate(int id, string displayNameAR, string displayNameEN, string userName, string logo)
        {
            var ministry = await _context.Ministries.FindAsync(id);
            if (ministry == null)
                return Json(new { success = false, message = "Ministry not found" });

            if (string.IsNullOrWhiteSpace(displayNameAR) && string.IsNullOrWhiteSpace(displayNameEN))
                return Json(new { success = false, message = "Display Name (Arabic or English) is required" });

            ministry.MinistryDisplayName_AR = displayNameAR;
            ministry.MinistryDisplayName_EN = displayNameEN;
            ministry.MinistryUserName = userName;
            ministry.Logo = logo;

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
            var ministry = await _context.Ministries.FindAsync(id);
            if (ministry == null)
                return Json(new { success = false, message = "Ministry not found" });

            try
            {
                _context.Ministries.Remove(ministry);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Ministries/PerformanceBreakdown/5
        public async Task<IActionResult> PerformanceBreakdown(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ministry = await _context.Ministries
                .Include(m => m.Projects)
                    .ThenInclude(p => p.ProjectIndicators)
                        .ThenInclude(pi => pi.Indicator)
                            .ThenInclude(i => i.Measures)
                .FirstOrDefaultAsync(m => m.Code == id);

            if (ministry == null)
            {
                return NotFound();
            }

            // Calculate the performance breakdown
            var breakdown = new List<dynamic>();
            double totalWeightedTarget = 0.0;
            double totalWeightedAchieved = 0.0;

            foreach (var project in ministry.Projects)
            {
                var projectBreakdown = new
                {
                    ProjectId = project.ProjectID,
                    ProjectName = project.ProjectName,
                    Indicators = project.ProjectIndicators.Select(pi => new
                    {
                        IndicatorCode = pi.Indicator.IndicatorCode,
                        IndicatorName = pi.Indicator.Name,
                        Weight = pi.Indicator.Weight > 0 ? pi.Indicator.Weight : 1,
                        Target = pi.Indicator.Target,
                        Achieved = pi.Indicator.Measures.Sum(m => m.Value),
                        WeightedTarget = pi.Indicator.Target * (pi.Indicator.Weight > 0 ? pi.Indicator.Weight : 1),
                        WeightedAchieved = pi.Indicator.Measures.Sum(m => m.Value) * (pi.Indicator.Weight > 0 ? pi.Indicator.Weight : 1),
                        Performance = pi.Indicator.Target > 0
                            ? (pi.Indicator.Measures.Sum(m => m.Value) / pi.Indicator.Target) * 100
                            : 0,
                        Measures = pi.Indicator.Measures.Select(m => new
                        {
                            MeasureCode = m.Code,
                            Value = m.Value,
                            Date = m.Date
                        }).ToList()
                    }).ToList()
                };

                breakdown.Add(projectBreakdown);

                // Aggregate totals
                foreach (var indicator in projectBreakdown.Indicators)
                {
                    totalWeightedTarget += indicator.WeightedTarget;
                    totalWeightedAchieved += indicator.WeightedAchieved;
                }
            }

            double calculatedPerformance = totalWeightedTarget > 0
                ? (totalWeightedAchieved / totalWeightedTarget) * 100
                : 0;

            ViewBag.Breakdown = breakdown;
            ViewBag.TotalWeightedTarget = totalWeightedTarget;
            ViewBag.TotalWeightedAchieved = totalWeightedAchieved;
            ViewBag.CalculatedPerformance = Math.Round(calculatedPerformance, 2);

            return View(ministry);
        }
    }
}
