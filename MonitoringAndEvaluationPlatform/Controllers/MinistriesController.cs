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
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class MinistriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly ICurrencyConversionService _currencyConversion;
        private readonly IMinistryScopeService _ministryScope;

        public MinistriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ICurrencyConversionService currencyConversion, IMinistryScopeService ministryScope)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _currencyConversion = currencyConversion;
            _ministryScope = ministryScope;
        }
        // GET: Ministries
        [Permission(Permissions.ReadMinistries)]
        public async Task<IActionResult> Index()
        {
            var scope = await _ministryScope.GetScopeAsync();
            var ministries = await _context.Ministries.WithinScope(scope).ToListAsync();
            return View(ministries);
        }

        // GET: Ministries
        public async Task<IActionResult> ResultIndex(int? ministryCode)
        {
            var scope = await _ministryScope.GetScopeAsync();

            // AsNoTracking: each ministry's Projects is replaced below with the projects it OWNS,
            // and that must never be saved back onto the ProjectMinistries mirror.
            IQueryable<Ministry> query = _context.Ministries.AsNoTracking().WithinScope(scope);

            if (ministryCode.HasValue)
            {
                // Show only the ministry with the given code
                query = query.Where(m => m.Code == ministryCode.Value);
            }

            var ministries = await query.ToListAsync();
            var codes = ministries.Select(m => m.Code).ToList();

            var ownedProjects = await _context.Projects
                .AsNoTracking()
                .WithinScope(scope)
                .Where(p => p.MinistryCode != null && codes.Contains(p.MinistryCode.Value))
                .Include(p => p.Sector)
                .Include(p => p.Donors)
                .Include(p => p.ProjectManager)
                .Include(p => p.SuperVisor)
                .ToListAsync();

            foreach (var ministry in ministries)
            {
                ministry.Projects = ownedProjects.Where(p => p.MinistryCode == ministry.Code).ToList();
            }

            // Calculate overall statistics
            var allProjects = ownedProjects;
            ViewBag.TotalProjects = allProjects.Count;
            ViewBag.ActiveProjects = allProjects.Count(p => p.EndDate >= DateTime.Now);
            ViewBag.CompletedProjects = allProjects.Count(p => p.EndDate < DateTime.Now);
            ViewBag.TotalBudget = (await _currencyConversion.GetConverterAsync()).SumBudget(allProjects);

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

            if (!await _ministryScope.CanAccessMinistryAsync(ministry.Code))
            {
                return Forbid();
            }

            // Get the projects this ministry owns
            var projects = await _context.Projects
                .Include(p => p.Sector)
                .Include(p => p.Donors)
                .Include(p => p.ProjectManager)
                .Include(p => p.SuperVisor)
                .Where(p => p.MinistryCode == id)
                .ToListAsync();

            // Calculate statistics
            ViewBag.TotalProjects = projects.Count;
            ViewBag.ActiveProjects = projects.Count(p => p.EndDate >= DateTime.Now);
            ViewBag.CompletedProjects = projects.Count(p => p.EndDate < DateTime.Now);
            ViewBag.TotalBudget = (await _currencyConversion.GetConverterAsync()).SumBudget(projects);
            ViewBag.Projects = projects;

            // Get ministry users: account administration, so administrators only.
            var ministryUsers = User.IsInRole(UserRoles.SystemAdministrator)
                ? await _userManager.Users
                    .Where(u => u.MinistryCode == ministry.Code)
                    .ToListAsync()
                : new List<ApplicationUser>();
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
                string defaultPassword = "Ministry@123";  // ⚠️ Change in production
                var user = new ApplicationUser
                {
                    UserName = ministry.MinistryUserName,
                    Email = $"{ministry.MinistryUserName.ToLower()}@example.com", // Example email
                    EmailConfirmed = true,
                    MinistryName = ministry.MinistryDisplayName_EN,
                    // Ministry scoping reads MinistryCode; without it the account sees nothing.
                    MinistryCode = ministry.Code
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

            // AsNoTracking: Projects is replaced below with the projects the ministry OWNS, and
            // that must never be saved back onto the ProjectMinistries mirror.
            var ministry = await _context.Ministries
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Code == id);

            if (ministry == null)
            {
                return NotFound();
            }

            if (!await _ministryScope.CanAccessMinistryAsync(ministry.Code))
            {
                return Forbid();
            }

            ministry.Projects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.MinistryCode == ministry.Code)
                .Include(p => p.Indicators)
                .Include(p => p.Phases)
                    .ThenInclude(pp => pp.Measures)
                .ToListAsync();

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
