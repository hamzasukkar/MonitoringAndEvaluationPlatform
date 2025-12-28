using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModels;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize(Roles = UserRoles.SystemAdministrator)]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AuditLogs
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            string? entityFilter = null,
            string? actionFilter = null,
            string? userFilter = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? sortColumn = "Timestamp",
            string? sortDirection = "desc",
            int page = 1,
            int pageSize = 25)
        {
            var query = _context.AuditLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a =>
                    a.EntityName.Contains(searchTerm) ||
                    a.EntityId.Contains(searchTerm) ||
                    (a.UserName != null && a.UserName.Contains(searchTerm)) ||
                    (a.OldValues != null && a.OldValues.Contains(searchTerm)) ||
                    (a.NewValues != null && a.NewValues.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(entityFilter))
            {
                query = query.Where(a => a.EntityName == entityFilter);
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action == actionFilter);
            }

            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                query = query.Where(a => a.UserId == userFilter);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(a => a.Timestamp >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                var endDate = dateTo.Value.AddDays(1);
                query = query.Where(a => a.Timestamp < endDate);
            }

            var totalRecords = await query.CountAsync();

            // Apply sorting
            query = sortColumn?.ToLower() switch
            {
                "timestamp" => sortDirection == "asc" ? query.OrderBy(a => a.Timestamp) : query.OrderByDescending(a => a.Timestamp),
                "user" => sortDirection == "asc" ? query.OrderBy(a => a.UserName) : query.OrderByDescending(a => a.UserName),
                "action" => sortDirection == "asc" ? query.OrderBy(a => a.Action) : query.OrderByDescending(a => a.Action),
                "entity" => sortDirection == "asc" ? query.OrderBy(a => a.EntityName) : query.OrderByDescending(a => a.EntityName),
                "entityid" => sortDirection == "asc" ? query.OrderBy(a => a.EntityId) : query.OrderByDescending(a => a.EntityId),
                _ => query.OrderByDescending(a => a.Timestamp)
            };

            var auditLogs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get statistics
            var allLogsQuery = _context.AuditLogs;
            var today = DateTime.UtcNow.Date;

            var viewModel = new AuditLogViewModel
            {
                AuditLogs = auditLogs,
                SearchTerm = searchTerm,
                EntityFilter = entityFilter,
                ActionFilter = actionFilter,
                UserFilter = userFilter,
                DateFrom = dateFrom,
                DateTo = dateTo,
                SortColumn = sortColumn,
                SortDirection = sortDirection,
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                AvailableEntities = await _context.AuditLogs
                    .Select(a => a.EntityName)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync(),
                AvailableUsers = await _context.AuditLogs
                    .Where(a => a.UserId != null)
                    .Select(a => new { a.UserId, a.UserName })
                    .Distinct()
                    .OrderBy(u => u.UserName)
                    .Select(u => u.UserId!)
                    .ToListAsync(),
                TotalAuditLogs = await allLogsQuery.CountAsync(),
                TodayLogs = await allLogsQuery.CountAsync(a => a.Timestamp >= today),
                CreateCount = await allLogsQuery.CountAsync(a => a.Action == "Create"),
                UpdateCount = await allLogsQuery.CountAsync(a => a.Action == "Update"),
                DeleteCount = await allLogsQuery.CountAsync(a => a.Action == "Delete")
            };

            return View(viewModel);
        }

        // GET: AuditLogs/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var auditLog = await _context.AuditLogs.FindAsync(id);

            if (auditLog == null)
            {
                return NotFound();
            }

            return View(auditLog);
        }

        // GET: AuditLogs/EntityHistory
        public async Task<IActionResult> EntityHistory(string entityName, string entityId)
        {
            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityName == entityName && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            ViewBag.EntityName = entityName;
            ViewBag.EntityId = entityId;

            return View(auditLogs);
        }

        // GET: AuditLogs/UserActivity
        public async Task<IActionResult> UserActivity(string userId, int page = 1, int pageSize = 25)
        {
            var query = _context.AuditLogs
                .Where(a => a.UserId == userId);

            var totalRecords = await query.CountAsync();

            var auditLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userName = auditLogs.FirstOrDefault()?.UserName ?? userId;

            ViewBag.UserId = userId;
            ViewBag.UserName = userName;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            return View(auditLogs);
        }

        // POST: AuditLogs/Export
        [HttpPost]
        public async Task<IActionResult> Export(
            string? entityFilter = null,
            string? actionFilter = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityFilter))
                query = query.Where(a => a.EntityName == entityFilter);

            if (!string.IsNullOrWhiteSpace(actionFilter))
                query = query.Where(a => a.Action == actionFilter);

            if (dateFrom.HasValue)
                query = query.Where(a => a.Timestamp >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(a => a.Timestamp < dateTo.Value.AddDays(1));

            var auditLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(10000) // Limit export to 10,000 records
                .ToListAsync();

            // Generate CSV
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,EntityName,EntityId,Action,UserName,Timestamp,ChangedColumns,OldValues,NewValues");

            foreach (var log in auditLogs)
            {
                csv.AppendLine($"\"{log.Id}\",\"{log.EntityName}\",\"{log.EntityId}\",\"{log.Action}\",\"{log.UserName}\",\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.ChangedColumns}\",\"{EscapeCsv(log.OldValues)}\",\"{EscapeCsv(log.NewValues)}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"audit_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        private string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
        }
    }
}
