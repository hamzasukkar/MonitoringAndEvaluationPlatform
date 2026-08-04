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
            int pageSize = 25,
            bool includeAuthentication = false)
        {
            var query = _context.AuditLogs.AsQueryable();

            // Login/logout events flood the list; hide them unless explicitly requested
            if (!includeAuthentication && string.IsNullOrWhiteSpace(entityFilter))
            {
                query = query.Where(a => a.EntityName != "Authentication");
            }

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a =>
                    a.EntityName.Contains(searchTerm) ||
                    a.EntityId.Contains(searchTerm) ||
                    (a.EntityDisplayName != null && a.EntityDisplayName.Contains(searchTerm)) ||
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
                IncludeAuthentication = includeAuthentication,
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

        // GET: AuditLogs/ProjectHistory/5
        // Comprehensive audit trail for one project: the Project row itself plus all its
        // child items (phases, measures, action plans, plans, donor links, files, indicators).
        public async Task<IActionResult> ProjectHistory(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();

            var phaseIds = await _context.ProjectPhases
                .Where(p => p.ProjectID == projectId).Select(p => p.Id).ToListAsync();
            var measureCodes = await _context.Measures
                .Where(m => phaseIds.Contains(m.ProjectPhaseId)).Select(m => m.Code).ToListAsync();
            var actionPlanCodes = await _context.ActionPlans
                .Where(a => phaseIds.Contains(a.ProjectPhaseId)).Select(a => a.Code).ToListAsync();
            var planCodes = await _context.Plans
                .Where(p => actionPlanCodes.Contains(p.ActionPlanCode)).Select(p => p.Code).ToListAsync();
            var donorIds = await _context.ProjectDonors
                .Where(d => d.ProjectId == projectId).Select(d => d.Id).ToListAsync();
            var fileIds = await _context.ProjectFiles
                .Where(f => f.ProjectId == projectId).Select(f => f.Id).ToListAsync();
            var indicatorCodes = await _context.Indicators
                .Where(i => i.ProjectID == projectId).Select(i => i.IndicatorCode).ToListAsync();

            var projectIdStr = projectId.ToString();
            var phaseIdStrs = phaseIds.Select(x => x.ToString()).ToList();
            var measureStrs = measureCodes.Select(x => x.ToString()).ToList();
            var actionPlanStrs = actionPlanCodes.Select(x => x.ToString()).ToList();
            var planStrs = planCodes.Select(x => x.ToString()).ToList();
            var donorStrs = donorIds.Select(x => x.ToString()).ToList();
            var fileStrs = fileIds.Select(x => x.ToString()).ToList();
            var indicatorStrs = indicatorCodes.Select(x => x.ToString()).ToList();

            // Note: audit rows of children deleted before this page is viewed are not included
            // (IDs are collected from live rows). Follow-up: denormalize ProjectId onto AuditLog
            // in the interceptor.
            var auditLogs = await _context.AuditLogs.Where(a =>
                    (a.EntityName == nameof(Project) && a.EntityId == projectIdStr) ||
                    (a.EntityName == nameof(ProjectPhase) && phaseIdStrs.Contains(a.EntityId)) ||
                    (a.EntityName == nameof(Measure) && measureStrs.Contains(a.EntityId)) ||
                    (a.EntityName == nameof(ActionPlan) && actionPlanStrs.Contains(a.EntityId)) ||
                    (a.EntityName == nameof(Plan) && planStrs.Contains(a.EntityId)) ||
                    (a.EntityName == nameof(ProjectDonor) && donorStrs.Contains(a.EntityId)) ||
                    (a.EntityName == nameof(ProjectFile) && fileStrs.Contains(a.EntityId)) ||
                    (a.EntityName == nameof(Indicator) && indicatorStrs.Contains(a.EntityId)))
                .OrderByDescending(a => a.Timestamp)
                .Take(1000)
                .ToListAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project.ProjectName;

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
            DateTime? dateTo = null,
            bool includeAuthentication = false)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!includeAuthentication && string.IsNullOrWhiteSpace(entityFilter))
                query = query.Where(a => a.EntityName != "Authentication");

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

            var escaped = value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");

            // CSV formula injection: Excel and Calc execute a cell that begins with =, +, -,
            // @, or a leading tab/CR. Audit values are attacker-influenced (they are just
            // user input echoed back), and this export is opened by an administrator, so a
            // crafted project name could run a formula on their workstation. Prefixing with
            // a single quote makes the cell inert while still displaying the original text.
            if (escaped.Length > 0 && "=+-@\t\r".Contains(escaped[0]))
            {
                escaped = "'" + escaped;
            }

            return escaped;
        }
    }
}
