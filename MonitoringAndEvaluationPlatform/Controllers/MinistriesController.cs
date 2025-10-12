using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
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

            var Ministry = await _context.Ministries
                .FirstOrDefaultAsync(m => m.Code == id);
            if (Ministry == null)
            {
                return NotFound();
            }

            return View(Ministry);
        }

        // GET: Ministries/Create
        public IActionResult Create()
        {
            return View();
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

     
        // GET: Ministries/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var Ministry = await _context.Ministries.FindAsync(id);
            if (Ministry == null)
            {
                return NotFound();
            }
            return View(Ministry);
        }

        // POST: Ministries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,Ministry Ministry)
        {
            if (id != Ministry.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(Ministry);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MinistryExists(Ministry.Code))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(Ministry);
        }

        // GET: Ministries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var Ministry = await _context.Ministries
                .FirstOrDefaultAsync(m => m.Code == id);
            if (Ministry == null)
            {
                return NotFound();
            }

            return View(Ministry);
        }

        // POST: Ministries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var Ministry = await _context.Ministries.FindAsync(id);
            if (Ministry != null)
            {
                _context.Ministries.Remove(Ministry);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
    }
}
