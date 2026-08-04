using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModels.DataManagement;
using System.Text;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize(Roles = UserRoles.SystemAdministrator)]
    public class DataManagementController : Controller
    {
        private readonly IDataManagementService _dataManagementService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public DataManagementController(
            IDataManagementService dataManagementService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _dataManagementService = dataManagementService;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: DataManagement
        public async Task<IActionResult> Index()
        {
            var model = await _dataManagementService.GetIndexStatisticsAsync();
            return View(model);
        }

        // GET: DataManagement/Backup
        public async Task<IActionResult> Backup()
        {
            var stats = await _dataManagementService.GetIndexStatisticsAsync();
            var model = new BackupViewModel
            {
                TotalRecords = stats.TotalProjects + stats.TotalFrameworks + stats.TotalIndicators +
                              stats.TotalPlans + stats.TotalMeasures + stats.TotalAuditLogs
            };
            return View(model);
        }

        // POST: DataManagement/ExecuteBackup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteBackup(BackupViewModel model, string password, string confirmationPhrase)
        {
            if (!await ValidateSecurityConfirmation(password, confirmationPhrase, "BACKUP DATABASE"))
            {
                TempData["ErrorMessage"] = "Invalid password or confirmation phrase.";
                return RedirectToAction(nameof(Backup));
            }

            try
            {
                var script = await _dataManagementService.GenerateDatabaseBackupScriptAsync(
                    model.IncludeIdentityData, model.IncludeAuditLogs);

                var bytes = Encoding.UTF8.GetBytes(script);
                var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";

                await LogAuditAction("Backup", "Database", "Generated backup script");

                return File(bytes, "application/sql", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Backup failed: {ex.Message}";
                return RedirectToAction(nameof(Backup));
            }
        }

        // POST: DataManagement/ExecuteNativeBackup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteNativeBackup(string password, string confirmationPhrase)
        {
            if (!await ValidateSecurityConfirmation(password, confirmationPhrase, "BACKUP DATABASE"))
            {
                TempData["ErrorMessage"] = "Invalid password or confirmation phrase.";
                return RedirectToAction(nameof(Backup));
            }

            try
            {
                var (filePath, fileName) = await _dataManagementService.CreateNativeDatabaseBackupAsync();

                var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

                await LogAuditAction("NativeBackup", "Database", $"Created native .bak: {fileName}");

                return File(bytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Native backup failed: {ex.Message}";
                return RedirectToAction(nameof(Backup));
            }
        }

        // GET: DataManagement/ClearData
        public async Task<IActionResult> ClearData()
        {
            var model = await _dataManagementService.GetTableGroupsAsync();
            return View(model);
        }

        // POST: DataManagement/ExecuteClearData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteClearData(List<string> selectedGroups, string password, string confirmationPhrase)
        {
            if (!await ValidateSecurityConfirmation(password, confirmationPhrase, "DELETE ALL DATA"))
            {
                TempData["ErrorMessage"] = "Invalid password or confirmation phrase.";
                return RedirectToAction(nameof(ClearData));
            }

            if (selectedGroups == null || !selectedGroups.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one table group to clear.";
                return RedirectToAction(nameof(ClearData));
            }

            try
            {
                var deletedCount = await _dataManagementService.ClearTablesAsync(selectedGroups);
                await LogAuditAction("ClearData", "Database", $"Cleared {deletedCount} records from groups: {string.Join(", ", selectedGroups)}");

                TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} records from selected tables.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Clear data failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: DataManagement/DeleteProject
        public async Task<IActionResult> DeleteProject(string? searchTerm = null)
        {
            var model = await _dataManagementService.GetProjectsForDeletionAsync(searchTerm);
            return View(model);
        }

        // POST: DataManagement/ExecuteDeleteProject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteDeleteProject(List<int> selectedProjectIds, string password, string confirmationPhrase)
        {
            if (!await ValidateSecurityConfirmation(password, confirmationPhrase, "DELETE PROJECT"))
            {
                TempData["ErrorMessage"] = "Invalid password or confirmation phrase.";
                return RedirectToAction(nameof(DeleteProject));
            }

            if (selectedProjectIds == null || !selectedProjectIds.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one project to delete.";
                return RedirectToAction(nameof(DeleteProject));
            }

            try
            {
                var deletedCount = await _dataManagementService.DeleteProjectsAsync(selectedProjectIds);
                await LogAuditAction("DeleteProject", "Project", $"Deleted {selectedProjectIds.Count} projects ({deletedCount} total records)");

                TempData["SuccessMessage"] = $"Successfully deleted {selectedProjectIds.Count} project(s) and {deletedCount} related records.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Delete projects failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: DataManagement/ResetValues
        public async Task<IActionResult> ResetValues()
        {
            var model = await _dataManagementService.GetResetValuesStatisticsAsync();
            return View(model);
        }

        // POST: DataManagement/ExecuteResetValues
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteResetValues(ResetValuesViewModel model, string password, string confirmationPhrase)
        {
            if (!await ValidateSecurityConfirmation(password, confirmationPhrase, "RESET ALL VALUES"))
            {
                TempData["ErrorMessage"] = "Invalid password or confirmation phrase.";
                return RedirectToAction(nameof(ResetValues));
            }

            if (!model.ResetPlans && !model.ResetMeasures && !model.ResetProjectPerformance && !model.RecalculateHierarchy)
            {
                TempData["ErrorMessage"] = "Please select at least one reset option.";
                return RedirectToAction(nameof(ResetValues));
            }

            try
            {
                var resetCount = await _dataManagementService.ResetValuesAsync(
                    model.ResetPlans, model.ResetMeasures, model.ResetProjectPerformance, model.RecalculateHierarchy);

                var actions = new List<string>();
                if (model.ResetPlans) actions.Add("Plans");
                if (model.ResetMeasures) actions.Add("Measures");
                if (model.ResetProjectPerformance) actions.Add("Project Performance");
                if (model.RecalculateHierarchy) actions.Add("Hierarchy Recalculation");

                await LogAuditAction("ResetValues", "Database", $"Reset operations: {string.Join(", ", actions)}. Affected records: {resetCount}");

                TempData["SuccessMessage"] = $"Successfully reset values. Affected records: {resetCount}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Reset values failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ValidateSecurityConfirmation(string password, string confirmationPhrase, string expectedPhrase)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmationPhrase))
            {
                await LogAuditAction("SecurityConfirmationFailed", "DataManagement", $"Missing password or phrase for '{expectedPhrase}'");
                return false;
            }

            if (!confirmationPhrase.Equals(expectedPhrase, StringComparison.OrdinalIgnoreCase))
            {
                await LogAuditAction("SecurityConfirmationFailed", "DataManagement", $"Incorrect confirmation phrase for '{expectedPhrase}'");
                return false;
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return false;

            // lockoutOnFailure: true - this endpoint re-checks the caller's own password, so
            // with it false a hijacked admin session was an unlimited password-guessing oracle.
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                // A failed step-up on a destructive operation is the strongest breach signal
                // this application has; it was previously not recorded at all.
                await LogAuditAction("SecurityConfirmationFailed", "DataManagement", $"Incorrect password for '{expectedPhrase}'");
            }

            return result.Succeeded;
        }

        private async Task LogAuditAction(string action, string entityName, string details)
        {
            var user = await _userManager.GetUserAsync(User);
            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = "DataManagement",
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
