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
            IQueryable<Ministry> query = _context.Ministries;

            if (ministryCode.HasValue)
            {
                // Show only the ministry with the given code
                query = query.Where(m => m.Code == ministryCode.Value);
            }

            var ministries = await query.ToListAsync();
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
        public async Task<IActionResult> CreateInline(string MinistryDisplayName, string MinistryUserName)
        {
            if (string.IsNullOrWhiteSpace(MinistryDisplayName))
            {
                return Json(new { success = false, message = "Display Name is required." });
            }

            var ministry = new Ministry
            {
                MinistryDisplayName = MinistryDisplayName,
                MinistryUserName = MinistryUserName ?? MinistryDisplayName.Replace(" ", "").ToLower()
            };

            try
            {
                _context.Ministries.Add(ministry);
                await _context.SaveChangesAsync();
                return Json(new { success = true, ministry = new { ministry.Code, ministry.MinistryDisplayName, ministry.MinistryUserName } });
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
                case "ministrydisplayname":
                    ministry.MinistryDisplayName = value;
                    break;
                case "ministryusername":
                    ministry.MinistryUserName = value;
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
        public async Task<IActionResult> QuickUpdate(int id, string displayName, string userName)
        {
            var ministry = await _context.Ministries.FindAsync(id);
            if (ministry == null)
                return Json(new { success = false, message = "Ministry not found" });

            if (string.IsNullOrWhiteSpace(displayName))
                return Json(new { success = false, message = "Display Name is required" });

            ministry.MinistryDisplayName = displayName;
            ministry.MinistryUserName = userName;

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
    }
}
