using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        private readonly ILogger<MinistriesController> _logger;

        public MinistriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<MinistriesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        /// <summary>
        /// Generates a random initial password that satisfies the configured Identity policy
        /// (length, upper, lower, digit, non-alphanumeric). Shown to the administrator once;
        /// the account is flagged MustChangePassword so it cannot be used long-term.
        /// </summary>
        private static string GenerateInitialPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%^&*-_=+";
            const string all = upper + lower + digits + symbols;

            // Guarantee one character from each required class, then fill to length 16.
            var chars = new List<char>
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                symbols[RandomNumberGenerator.GetInt32(symbols.Length)]
            };

            while (chars.Count < 16)
            {
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
            }

            // Fisher-Yates shuffle so the guaranteed characters are not always in front.
            for (int i = chars.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
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
        [Permission(Permissions.CreateMinistry)]
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
                // The password is generated, shown to the administrator exactly once, and must
                // be changed on first sign-in. It used to be the literal "Ministry@123", which
                // meant every ministry account shared one publicly-known password.
                string generatedPassword = GenerateInitialPassword();
                var user = new ApplicationUser
                {
                    UserName = ministry.MinistryUserName,
                    Email = $"{ministry.MinistryUserName.ToLower()}@example.com", // Example email
                    EmailConfirmed = true,
                    MinistryName = ministry.MinistryUserName,
                    MustChangePassword = true
                };

                var result = await _userManager.CreateAsync(user, generatedPassword);
                if (result.Succeeded)
                {
                    // 🔹 Assign User to Role
                    await _userManager.AddToRoleAsync(user, ministry.MinistryUserName);
                    TempData["GeneratedPassword"] =
                        $"Initial password for '{user.UserName}': {generatedPassword} — copy it now, it will not be shown again.";
                }
                else
                {
                    _logger.LogWarning("Ministry user creation failed for {UserName}: {Errors}",
                        ministry.MinistryUserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "The ministry was created but its user account could not be created.";
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
        [Permission(Permissions.CreateMinistry)]
        public async Task<IActionResult> CreateInline(string MinistryDisplayName_AR, string MinistryDisplayName_EN, string MinistryUserName, string Logo)
        {
            if (string.IsNullOrWhiteSpace(MinistryDisplayName_AR) && string.IsNullOrWhiteSpace(MinistryDisplayName_EN))
            {
                return Json(new { success = false, message = "Display Name (Arabic or English) is required." });
            }

            var ministry = new Ministry
            {
                MinistryDisplayName_AR = MinistryDisplayName_AR ?? "",
                MinistryDisplayName_EN = MinistryDisplayName_EN ?? "",
                MinistryUserName = MinistryUserName ?? (MinistryDisplayName_EN ?? MinistryDisplayName_AR).Replace(" ", "").ToLower(),
                Logo = Logo ?? ""
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
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyMinistry)]
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
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyMinistry)]
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
        [ValidateAntiForgeryToken]
        [Permission(Permissions.DeleteMinistry)]
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
                    .ThenInclude(p => p.Indicators)
                .Include(m => m.Projects)
                    .ThenInclude(p => p.Phases)
                        .ThenInclude(pp => pp.Measures)
                .FirstOrDefaultAsync(m => m.Code == id);

            if (ministry == null)
            {
                return NotFound();
            }

            // Calculate the performance breakdown.
            // Per-indicator Performance % = project.performance (passthrough — same as
            // how indicator performance is computed everywhere else in the system,
            // see MonitoringService.UpdateIndicatorsForProject).
            // Final Ministry Performance = simple average of project performances,
            // mirroring MonitoringService.UpdateMinistryPerformanceByMinistryCode.
            var breakdown = new List<dynamic>();

            foreach (var project in ministry.Projects)
            {
                var projectMeasures = project.Phases.SelectMany(pp => pp.Measures).ToList();
                double projectPerformance = project.performance;

                var projectBreakdown = new
                {
                    ProjectId = project.ProjectID,
                    ProjectName = project.ProjectName,
                    ProjectPerformance = projectPerformance,
                    Indicators = project.Indicators.Select(i => new
                    {
                        IndicatorCode = i.IndicatorCode,
                        IndicatorName = i.Name,
                        Weight = i.Weight > 0 ? i.Weight : 1,
                        Target = i.Target,
                        Achieved = projectMeasures.Sum(m => m.Value),
                        Performance = projectPerformance,
                        Measures = projectMeasures.Select(m => new
                        {
                            MeasureCode = m.Code,
                            Value = m.Value,
                            Date = m.Date
                        }).ToList()
                    }).ToList()
                };

                breakdown.Add(projectBreakdown);
            }

            double calculatedPerformance = ministry.Projects.Any()
                ? ministry.Projects.Average(p => p.performance)
                : 0;

            ViewBag.Breakdown = breakdown;
            ViewBag.ProjectCount = ministry.Projects.Count;
            ViewBag.CalculatedPerformance = Math.Round(calculatedPerformance, 2);

            return View(ministry);
        }
    }
}
