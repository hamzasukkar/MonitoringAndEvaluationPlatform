using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModels;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize(Roles = UserRoles.SystemAdministrator)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ITestDataGeneratorService _testDataGenerator;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ITestDataGeneratorService testDataGenerator)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _testDataGenerator = testDataGenerator;
        }

        // GET: Admin/Test - Simple test to check if controller works
        public IActionResult Test()
        {
            return Content("Admin controller is working! User: " + User.Identity?.Name);
        }

        // GET: Admin/Index
        public async Task<IActionResult> Index(string searchTerm = "", string roleFilter = "", string ministryFilter = "", int page = 1, int pageSize = 10)
        {
            return await GetUserManagementView(searchTerm, roleFilter, ministryFilter, page, pageSize, "Index");
        }

        // GET: Admin/Users (Alternative page with same functionality)
        public async Task<IActionResult> Users(string searchTerm = "", string roleFilter = "", string ministryFilter = "", int page = 1, int pageSize = 10)
        {
            return await GetUserManagementView(searchTerm, roleFilter, ministryFilter, page, pageSize, "Users");
        }

        // Shared method for user management
        private async Task<IActionResult> GetUserManagementView(string searchTerm, string roleFilter, string ministryFilter, int page, int pageSize, string viewName)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                usersQuery = usersQuery.Where(u =>
                    u.UserName!.Contains(searchTerm) ||
                    u.Email!.Contains(searchTerm) ||
                    (u.MinistryName != null && u.MinistryName.Contains(searchTerm)));
            }

            // Filter by role if specified (do this at database level)
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                var roleId = await _context.Roles
                    .Where(r => r.Name == roleFilter)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (roleId != null)
                {
                    var userIdsInRole = _context.UserRoles
                        .Where(ur => ur.RoleId == roleId)
                        .Select(ur => ur.UserId);

                    usersQuery = usersQuery.Where(u => userIdsInRole.Contains(u.Id));
                }
            }

            // Filter by ministry if specified
            if (!string.IsNullOrWhiteSpace(ministryFilter))
            {
                usersQuery = usersQuery.Where(u => u.MinistryName == ministryFilter);
            }

            var totalUsers = await usersQuery.CountAsync();

            var users = await usersQuery
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get all user IDs for batch role lookup
            var userIds = users.Select(u => u.Id).ToList();

            // Batch load all user roles in a single query
            var userRoles = await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, RoleName = r.Name })
                .ToListAsync();

            // Group roles by user ID
            var userRolesDict = userRoles
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName!).ToList());

            // Load ministries for Arabic name lookup
            var ministries = await _context.Ministries
                .Select(m => new { m.MinistryDisplayName_EN, m.MinistryDisplayName_AR })
                .ToListAsync();
            var ministryArDict = ministries.ToDictionary(m => m.MinistryDisplayName_EN, m => m.MinistryDisplayName_AR);

            // Build view models
            var userViewModels = users.Select(user => new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                MinistryName = user.MinistryName,
                MinistryNameAr = user.MinistryName != null && ministryArDict.TryGetValue(user.MinistryName, out var arName) ? arName : user.MinistryName,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                Roles = userRolesDict.TryGetValue(user.Id, out var roles) ? roles : new List<string>()
            }).ToList();

            var viewModel = new UserManagementViewModel
            {
                Users = userViewModels,
                SearchTerm = searchTerm,
                RoleFilter = roleFilter,
                MinistryFilter = ministryFilter,
                CurrentPage = page,
                PageSize = pageSize,
                TotalUsers = totalUsers,
                TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize),
                AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync(),
                AvailableMinistries = await _context.Ministries
                    .OrderBy(m => m.MinistryDisplayName_EN)
                    .ToListAsync()
            };

            return View(viewName, viewModel);
        }

        // GET: Admin/CreateUser
        public async Task<IActionResult> CreateUser()
        {
            var model = new CreateUserViewModel
            {
                AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync(),
                Ministries = await _context.Ministries.ToListAsync()
            };
            return View(model);
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    EmailConfirmed = model.EmailConfirmed,
                    MinistryName = model.MinistryName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign roles
                    if (model.SelectedRoles != null && model.SelectedRoles.Any())
                    {
                        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    }

                    TempData["SuccessMessage"] = $"User '{user.UserName}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            model.Ministries = await _context.Ministries.ToListAsync();
            return View(model);
        }

        // GET: Admin/EditUser/id
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                EmailConfirmed = user.EmailConfirmed,
                MinistryName = user.MinistryName,
                LockoutEnabled = user.LockoutEnabled,
                SelectedRoles = userRoles.ToList(),
                AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync(),
                Ministries = await _context.Ministries.ToListAsync()
            };

            return View(model);
        }

        // POST: Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                user.UserName = model.UserName;
                user.Email = model.Email;
                user.EmailConfirmed = model.EmailConfirmed;
                user.MinistryName = model.MinistryName;
                user.LockoutEnabled = model.LockoutEnabled;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update roles
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var rolesToRemove = currentRoles.Except(model.SelectedRoles ?? new List<string>()).ToList();
                    var rolesToAdd = (model.SelectedRoles ?? new List<string>()).Except(currentRoles).ToList();

                    if (rolesToRemove.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    }

                    if (rolesToAdd.Any())
                    {
                        await _userManager.AddToRolesAsync(user, rolesToAdd);
                    }

                    TempData["SuccessMessage"] = $"User '{user.UserName}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            model.Ministries = await _context.Ministries.ToListAsync();
            return View(model);
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = $"User '{user.UserName}' deleted successfully." });
            }

            return Json(new { success = false, message = "Failed to delete user." });
        }

        // POST: Admin/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = $"Password reset successfully for '{user.UserName}'." });
            }

            return Json(new { success = false, message = "Failed to reset password." });
        }

        // POST: Admin/ToggleLockout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockout(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                // Unlock user
                await _userManager.SetLockoutEndDateAsync(user, null);
                return Json(new { success = true, message = $"User '{user.UserName}' unlocked.", locked = false });
            }
            else
            {
                // Lock user for 100 years
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                return Json(new { success = true, message = $"User '{user.UserName}' locked.", locked = true });
            }
        }

        // GET: Admin/UserDetails/id
        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Get Arabic ministry name if available
            string? ministryNameAr = null;
            if (!string.IsNullOrEmpty(user.MinistryName))
            {
                ministryNameAr = await _context.Ministries
                    .Where(m => m.MinistryDisplayName_EN == user.MinistryName)
                    .Select(m => m.MinistryDisplayName_AR)
                    .FirstOrDefaultAsync();
            }

            var viewModel = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                MinistryName = user.MinistryName,
                MinistryNameAr = ministryNameAr ?? user.MinistryName,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                Roles = roles.ToList()
            };

            return View(viewModel);
        }

        // GET: Admin/Roles
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var roleViewModels = new List<RoleViewModel>();

            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                roleViewModels.Add(new RoleViewModel
                {
                    Id = role.Id,
                    Name = role.Name!,
                    UserCount = usersInRole.Count,
                    Permissions = RolePermissionService.GetPermissionsForRole(role.Name!)
                });
            }

            var viewModel = new RoleManagementViewModel
            {
                Roles = roleViewModels.OrderBy(r => r.Name).ToList(),
                TotalRoles = roles.Count,
                TotalUsers = await _userManager.Users.CountAsync()
            };

            return View(viewModel);
        }

        // GET: Admin/Formulas - Display all calculation formulas used in the platform
        public IActionResult Formulas()
        {
            return View();
        }

        // GET: Admin/RoleDetails/id
        public async Task<IActionResult> RoleDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            var userViewModels = new List<UserViewModel>();

            foreach (var user in usersInRole)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName!,
                    Email = user.Email!,
                    MinistryName = user.MinistryName,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = userRoles.ToList()
                });
            }

            var viewModel = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name!,
                UserCount = usersInRole.Count,
                Permissions = RolePermissionService.GetPermissionsForRole(role.Name!),
                Users = userViewModels.OrderBy(u => u.UserName).ToList()
            };

            return View(viewModel);
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEST DATA GENERATOR
        // Builds a full Framework → … → Plan tree under a chosen ministry so the
        // platform can be exercised without hand-entering dozens of forms.
        // ─────────────────────────────────────────────────────────────────────

        // GET: Admin/GenerateTestData
        public async Task<IActionResult> GenerateTestData()
        {
            var model = new GenerateTestDataViewModel();
            await PopulateMinistriesAsync(model);
            return View(model);
        }

        // POST: Admin/ExecuteGenerateTestData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteGenerateTestData(GenerateTestDataViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateMinistriesAsync(model);
                return View(nameof(GenerateTestData), model);
            }

            try
            {
                var result = await _testDataGenerator.GenerateAsync(model);
                await LogAuditAction("GenerateTestData", "TestData",
                    $"Generated under ministry {model.MinistryCode} with prefix '{model.NamePrefix}': {result}");

                TempData["SuccessMessage"] =
                    $"Generated {result.Total} records — {result}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Generation failed: {ex.Message}";
            }

            return RedirectToAction(nameof(GenerateTestData));
        }

        // POST: Admin/DeleteGeneratedTestData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGeneratedTestData(string prefix, string password, string confirmationPhrase)
        {
            if (!await ValidateSecurityConfirmation(password, confirmationPhrase,
                    GenerateTestDataViewModel.DeleteConfirmationPhrase))
            {
                TempData["ErrorMessage"] =
                    $"Invalid password or confirmation phrase. Type '{GenerateTestDataViewModel.DeleteConfirmationPhrase}' exactly.";
                return RedirectToAction(nameof(GenerateTestData));
            }

            try
            {
                var result = await _testDataGenerator.DeleteByPrefixAsync(prefix);

                if (result.Total == 0)
                {
                    TempData["ErrorMessage"] = $"No data found with the prefix '{prefix}'. Nothing was deleted.";
                }
                else
                {
                    await LogAuditAction("DeleteTestData", "TestData",
                        $"Deleted data with prefix '{prefix}': {result}");
                    TempData["SuccessMessage"] = $"Deleted {result.Total} records — {result}.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Deletion failed: {ex.Message}";
            }

            return RedirectToAction(nameof(GenerateTestData));
        }

        private async Task PopulateMinistriesAsync(GenerateTestDataViewModel model)
        {
            model.AvailableMinistries = await _context.Ministries
                .OrderBy(m => m.MinistryDisplayName_EN)
                .ToListAsync();
        }

        /// <summary>
        /// Two factors for destructive actions: the admin's own password re-verified, plus an
        /// exact typed phrase. Mirrors DataManagementController.ValidateSecurityConfirmation.
        /// </summary>
        private async Task<bool> ValidateSecurityConfirmation(string password, string confirmationPhrase, string expectedPhrase)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmationPhrase))
                return false;

            if (!confirmationPhrase.Equals(expectedPhrase, StringComparison.OrdinalIgnoreCase))
                return false;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return false;

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            return result.Succeeded;
        }

        private async Task LogAuditAction(string action, string entityName, string details)
        {
            var user = await _userManager.GetUserAsync(User);
            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = "Admin",
                Action = action,
                UserId = user?.Id,
                UserName = user?.UserName,
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Details = details })
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
