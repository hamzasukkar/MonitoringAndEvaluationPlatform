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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Admin/Index
        public async Task<IActionResult> Index(string searchTerm = "", string roleFilter = "", int page = 1, int pageSize = 10)
        {
            return await GetUserManagementView(searchTerm, roleFilter, page, pageSize, "Index");
        }

        // GET: Admin/Users (Alternative page with same functionality)
        public async Task<IActionResult> Users(string searchTerm = "", string roleFilter = "", int page = 1, int pageSize = 10)
        {
            return await GetUserManagementView(searchTerm, roleFilter, page, pageSize, "Users");
        }

        // Shared method for user management
        private async Task<IActionResult> GetUserManagementView(string searchTerm, string roleFilter, int page, int pageSize, string viewName)
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

            // Build view models
            var userViewModels = users.Select(user => new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                MinistryName = user.MinistryName,
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
                CurrentPage = page,
                PageSize = pageSize,
                TotalUsers = totalUsers,
                TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize),
                AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync()
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

            var viewModel = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                MinistryName = user.MinistryName,
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
    }
}
