using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using Microsoft.AspNetCore.Authorization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<RequestsController> _localizer;

        public RequestsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<RequestsController> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        // ---------------------------------------------------------------- Management

        // GET: Requests
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            RequestStatus? status = null,
            RequestPriority? priority = null,
            RequestCategory? category = null,
            int? versionId = null,
            int? employeeId = null)
        {
            var query = _context.Requests
                .Include(r => r.SubmittedByUser)
                .Include(r => r.AssignedToUser)
                .Include(r => r.PlatformVersion)
                .Include(r => r.RequestEmployee)
                .Include(r => r.Files)
                .Include(r => r.Tests).ThenInclude(t => t.RequestEmployee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r =>
                    r.RequestNumber.Contains(searchTerm) ||
                    r.Title.Contains(searchTerm) ||
                    (r.RequestingParty != null && r.RequestingParty.Contains(searchTerm)));
            }

            if (status.HasValue) query = query.Where(r => r.Status == status.Value);
            if (priority.HasValue) query = query.Where(r => r.Priority == priority.Value);
            if (category.HasValue) query = query.Where(r => r.Category == category.Value);
            if (versionId.HasValue) query = query.Where(r => r.PlatformVersionId == versionId.Value);
            if (employeeId.HasValue) query = query.Where(r => r.RequestEmployeeId == employeeId.Value);

            var requests = await query
                .OrderByDescending(r => r.RequestDate)
                .ThenByDescending(r => r.Id)
                .ToListAsync();

            // Stat cards replace the Excel dashboard sheet. Counted over all requests,
            // not the filtered set, so the totals stay stable while filtering.
            var all = await _context.Requests
                .Select(r => new { r.Status })
                .ToListAsync();

            ViewBag.TotalCount = all.Count;
            ViewBag.NewCount = all.Count(r => r.Status == RequestStatus.New);
            ViewBag.InProgressCount = all.Count(r => r.Status == RequestStatus.InProgress);
            ViewBag.OnHoldCount = all.Count(r => r.Status == RequestStatus.OnHold);
            ViewBag.CompletedCount = all.Count(r => r.Status == RequestStatus.Completed);
            ViewBag.CancelledCount = all.Count(r => r.Status == RequestStatus.Cancelled);
            ViewBag.CompletionRate = all.Count == 0
                ? 0
                : (int)Math.Round(all.Count(r => r.Status == RequestStatus.Completed) * 100.0 / all.Count);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.Category = category;
            ViewBag.VersionId = versionId;
            ViewBag.VersionFilter = new SelectList(
                await _context.PlatformVersions
                    .OrderByDescending(v => v.Id)
                    .Select(v => new { v.Id, v.VersionNumber })
                    .ToListAsync(),
                "Id", "VersionNumber", versionId);
            ViewBag.EmployeeId = employeeId;
            ViewBag.EmployeeFilter = new SelectList(
                await _context.RequestEmployees
                    .OrderBy(e => e.Name)
                    .Select(e => new { e.Id, e.Name })
                    .ToListAsync(),
                "Id", "Name", employeeId);

            // Full employee list for the shared "record a test" modal. Kept separate from
            // EmployeeFilter so the filter's selected value does not leak into the modal.
            ViewBag.TestEmployees = new SelectList(
                await _context.RequestEmployees
                    .OrderBy(e => e.Name)
                    .Select(e => new { e.Id, e.Name })
                    .ToListAsync(),
                "Id", "Name");

            return View(requests);
        }

        // ---------------------------------------------------------------- Submission

        // GET: Requests/Create
        [Permission(Permissions.SubmitRequest)]
        public async Task<IActionResult> Create()
        {
            await PopulateLookupsAsync();
            return View(new Request { RequestDate = DateTime.Today });
        }

        // POST: Requests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.SubmitRequest)]
        public async Task<IActionResult> Create(Request request, List<IFormFile> UploadedFiles)
        {
            // Server-owned fields: never trust posted values for these.
            // Qualified with Models. because bare `Request` binds to Controller.Request.
            ModelState.Remove(nameof(Models.Request.RequestNumber));
            ModelState.Remove(nameof(Models.Request.SubmittedByUserId));

            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync();
                return View(request);
            }

            var user = await _userManager.GetUserAsync(User);
            request.SubmittedByUserId = user?.Id;
            request.MinistryCode ??= user?.MinistryCode;
            request.Status = RequestStatus.New;
            request.RequestNumber = await GenerateRequestNumberAsync(request.RequestDate);

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            await ProcessFileUploadsAsync(request.Id, UploadedFiles);

            this.SetSuccessMessage(string.Format(
                _localizer["Request '{0}' has been submitted successfully."].Value, request.RequestNumber));

            return RedirectToAction(nameof(Details), new { id = request.Id });
        }

        // GET: Requests/MyRequests
        [Permission(Permissions.SubmitRequest)]
        public async Task<IActionResult> MyRequests()
        {
            var userId = _userManager.GetUserId(User);

            var requests = await _context.Requests
                .Include(r => r.AssignedToUser)
                .Include(r => r.RequestEmployee)
                .Include(r => r.Files)
                .Where(r => r.SubmittedByUserId == userId)
                .OrderByDescending(r => r.RequestDate)
                .ThenByDescending(r => r.Id)
                .ToListAsync();

            return View(requests);
        }

        // ---------------------------------------------------------------- Details / Edit

        // GET: Requests/Details/5
        [Permission(Permissions.ReadRequests)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.SubmittedByUser)
                .Include(r => r.AssignedToUser)
                .Include(r => r.Ministry)
                .Include(r => r.PlatformVersion)
                .Include(r => r.RequestEmployee)
                .Include(r => r.Files)
                .Include(r => r.Comments).ThenInclude(c => c.AuthorUser)
                .Include(r => r.Comments).ThenInclude(c => c.OnBehalfOfEmployee)
                .Include(r => r.Tests).ThenInclude(t => t.RequestEmployee)
                .Include(r => r.Tests).ThenInclude(t => t.RecordedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();
            if (!CanView(request)) return Forbid();

            // The discussion thread and quick-status control are open to anyone who
            // can view the request (submitter, assignee, admin).
            ViewBag.CanInteract = true;
            ViewBag.CurrentUserId = _userManager.GetUserId(User);
            ViewBag.IsAdmin = User.IsInRole(UserRoles.SystemAdministrator);
            ViewBag.CommentEmployees = new SelectList(
                await _context.RequestEmployees.OrderBy(e => e.Name)
                    .Select(e => new { e.Id, e.Name }).ToListAsync(),
                "Id", "Name");

            // Only employees who have not ticked this request yet are valid picks, so the
            // duplicate case is unreachable from the form and the unique index is a backstop.
            var testedIds = request.Tests.Select(t => t.RequestEmployeeId).ToList();
            ViewBag.TestEmployees = new SelectList(
                await _context.RequestEmployees
                    .Where(e => !testedIds.Contains(e.Id))
                    .OrderBy(e => e.Name)
                    .Select(e => new { e.Id, e.Name }).ToListAsync(),
                "Id", "Name");

            return View(request);
        }

        // GET: Requests/Edit/5
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.Files)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            await PopulateLookupsAsync(request.PlatformVersionId);
            return View(request);
        }

        // POST: Requests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> Edit(int id, Request request, List<IFormFile> UploadedFiles)
        {
            if (id != request.Id) return NotFound();

            ModelState.Remove(nameof(Models.Request.RequestNumber));
            ModelState.Remove(nameof(Models.Request.SubmittedByUserId));

            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync(request.PlatformVersionId);
                return View(request);
            }

            var existing = await _context.Requests.FirstOrDefaultAsync(r => r.Id == id);
            if (existing == null) return NotFound();

            // RequestNumber and SubmittedByUserId are immutable once assigned.
            existing.RequestDate = request.RequestDate;
            existing.MeetingDate = request.MeetingDate;
            existing.RequestingParty = request.RequestingParty;
            existing.Title = request.Title;
            existing.Description = request.Description;
            existing.Category = request.Category;
            existing.Priority = request.Priority;
            existing.AssignedToUserId = request.AssignedToUserId;
            existing.Status = request.Status;
            existing.StartDate = request.StartDate;
            existing.ActualCompletionDate = request.ActualCompletionDate;
            existing.CompletionPercentage = request.CompletionPercentage;
            existing.Notes = request.Notes;
            existing.MinistryCode = request.MinistryCode;
            existing.PlatformVersionId = request.PlatformVersionId;
            existing.RequestEmployeeId = request.RequestEmployeeId;

            await _context.SaveChangesAsync();
            await ProcessFileUploadsAsync(existing.Id, UploadedFiles);

            this.SetSuccessMessage(string.Format(
                _localizer["Request '{0}' has been updated successfully."].Value, existing.RequestNumber));

            return RedirectToAction(nameof(Details), new { id = existing.Id });
        }

        // POST: Requests/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.DeleteRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.Requests
                .Include(r => r.Files)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            foreach (var file in request.Files)
            {
                DeletePhysicalFile(file.FilePath);
            }

            _context.Requests.Remove(request);
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(string.Format(
                _localizer["Request '{0}' has been deleted."].Value, request.RequestNumber));

            return RedirectToAction(nameof(Index));
        }

        // ---------------------------------------------------------------- Discussion / quick status

        // POST: Requests/QuickStatus — status change for anyone who can view the request.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ReadRequests)]
        public async Task<IActionResult> QuickStatus(int requestId, RequestStatus status)
        {
            var request = await _context.Requests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return NotFound();
            if (!CanView(request)) return Forbid();

            request.Status = status;
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(_localizer["Status updated."].Value);
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        // POST: Requests/AddComment — testing feedback or a reply.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ReadRequests)]
        public async Task<IActionResult> AddComment(
            int requestId, int? parentId, CommentKind kind, int? onBehalfOfEmployeeId, string? body)
        {
            var request = await _context.Requests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return NotFound();
            if (!CanView(request)) return Forbid();

            // A general comment/reply must carry text; a "passed" test may stand alone.
            if (string.IsNullOrWhiteSpace(body) && kind != CommentKind.TestPassed)
            {
                this.SetErrorMessage(_localizer["The comment cannot be empty."].Value);
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            // A reply is always a plain comment regardless of the posted kind.
            if (parentId.HasValue)
            {
                var parentExists = await _context.RequestComments
                    .AnyAsync(c => c.Id == parentId.Value && c.RequestId == requestId);
                if (!parentExists) return NotFound();
                kind = CommentKind.Comment;
            }

            _context.RequestComments.Add(new RequestComment
            {
                RequestId = requestId,
                ParentCommentId = parentId,
                AuthorUserId = _userManager.GetUserId(User),
                OnBehalfOfEmployeeId = onBehalfOfEmployeeId,
                Kind = kind,
                Body = string.IsNullOrWhiteSpace(body) ? null : body.Trim()
            });
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(_localizer["Comment added."].Value);
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        // POST: Requests/DeleteComment — the author or an admin only.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ReadRequests)]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.RequestComments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();

            var isAuthor = comment.AuthorUserId == _userManager.GetUserId(User);
            if (!isAuthor && !User.IsInRole(UserRoles.SystemAdministrator)) return Forbid();

            // Remove replies first (parent FK is NoAction, so no automatic cascade).
            _context.RequestComments.RemoveRange(comment.Replies);
            _context.RequestComments.Remove(comment);
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(_localizer["Comment deleted."].Value);
            return RedirectToAction(nameof(Details), new { id = comment.RequestId });
        }

        // ---------------------------------------------------------------- Testing sign-off

        // POST: Requests/RecordTest — a named employee ticks "I tried this and it works".
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> RecordTest(
            int requestId, int requestEmployeeId, string? note, string? returnUrl)
        {
            var requestExists = await _context.Requests.AnyAsync(r => r.Id == requestId);
            if (!requestExists) return NotFound();

            var employee = await _context.RequestEmployees
                .FirstOrDefaultAsync(e => e.Id == requestEmployeeId);

            if (employee == null)
            {
                this.SetErrorMessage(_localizer["Select the employee who tested the request."].Value);
                return BackTo(returnUrl, requestId);
            }

            // The unique index is the real guard; this check turns the common case into a
            // friendly message instead of an exception page.
            var already = await _context.RequestTests
                .AnyAsync(t => t.RequestId == requestId && t.RequestEmployeeId == requestEmployeeId);

            if (already)
            {
                this.SetErrorMessage(string.Format(
                    _localizer["'{0}' has already recorded a test on this request."].Value, employee.Name));
                return BackTo(returnUrl, requestId);
            }

            _context.RequestTests.Add(new RequestTest
            {
                RequestId = requestId,
                RequestEmployeeId = requestEmployeeId,
                RecordedByUserId = _userManager.GetUserId(User),
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Two concurrent submits raced past the check above and hit the unique index.
                this.SetErrorMessage(string.Format(
                    _localizer["'{0}' has already recorded a test on this request."].Value, employee.Name));
                return BackTo(returnUrl, requestId);
            }

            this.SetSuccessMessage(string.Format(
                _localizer["Test by '{0}' has been recorded."].Value, employee.Name));

            return BackTo(returnUrl, requestId);
        }

        // POST: Requests/RemoveTest — undo a tick. ManageRequests is the whole
        // authorization story: a tick has no "author" who could self-service it,
        // unlike a comment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> RemoveTest(int id, string? returnUrl)
        {
            var test = await _context.RequestTests
                .Include(t => t.RequestEmployee)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null) return NotFound();

            // Read what the redirect and the message need before the entity is removed.
            var requestId = test.RequestId;
            var employeeName = test.RequestEmployee.Name;

            _context.RequestTests.Remove(test);
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(string.Format(
                _localizer["Test by '{0}' has been removed."].Value, employeeName));

            return BackTo(returnUrl, requestId);
        }

        // ---------------------------------------------------------------- Attachments

        // POST: Requests/DeleteFile/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await _context.RequestFiles.FirstOrDefaultAsync(f => f.Id == id);
            if (file == null) return NotFound();

            var requestId = file.RequestId;
            DeletePhysicalFile(file.FilePath);

            _context.RequestFiles.Remove(file);
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(_localizer["Attachment has been deleted."].Value);
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        // ---------------------------------------------------------------- Helpers

        /// <summary>
        /// REQ-yyyy-000, sequential within the request's year. Generated once and stored,
        /// so deleting or reordering rows never renumbers existing requests. The unique
        /// index on RequestNumber is the real guard against a concurrent duplicate.
        /// </summary>
        private async Task<string> GenerateRequestNumberAsync(DateTime requestDate)
        {
            var year = requestDate.Year;
            var prefix = $"REQ-{year}-";

            var lastNumber = await _context.Requests
                .Where(r => r.RequestNumber.StartsWith(prefix))
                .OrderByDescending(r => r.RequestNumber)
                .Select(r => r.RequestNumber)
                .FirstOrDefaultAsync();

            var next = 1;
            if (!string.IsNullOrEmpty(lastNumber))
            {
                var tail = lastNumber.Substring(prefix.Length);
                if (int.TryParse(tail, out var parsed))
                {
                    next = parsed + 1;
                }
            }

            return $"{prefix}{next:000}";
        }

        /// <summary>
        /// Mirrors ProjectsController.ProcessFileUploadsAsync: stores under
        /// wwwroot/uploads with a GUID-prefixed name and records the row.
        /// </summary>
        private async Task<bool> ProcessFileUploadsAsync(int requestId, List<IFormFile> uploadedFiles)
        {
            if (uploadedFiles == null || !uploadedFiles.Any())
                return true;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            foreach (var file in uploadedFiles)
            {
                if (file.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.RequestFiles.Add(new RequestFile
                    {
                        RequestId = requestId,
                        FileName = file.FileName,
                        FilePath = "/uploads/" + uniqueFileName
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private static void DeletePhysicalFile(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return;

            var relative = storedPath.TrimStart('/', '\\');
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        /// <summary>
        /// Admins see everything; everyone else only requests they submitted or own.
        /// </summary>
        private bool CanView(Request request)
        {
            if (User.IsInRole(UserRoles.SystemAdministrator)) return true;

            var userId = _userManager.GetUserId(User);
            return request.SubmittedByUserId == userId
                || request.AssignedToUserId == userId;
        }

        /// <summary>
        /// Returns to the page the tick was recorded from — the Index keeps its filters in
        /// the query string — and falls back to Details. Url.IsLocalUrl blocks an open
        /// redirect; unlike LocalRedirect it degrades instead of throwing.
        /// </summary>
        private IActionResult BackTo(string? returnUrl, int requestId)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        private async Task PopulateLookupsAsync(int? includeVersionId = null)
        {
            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            ViewBag.Users = new SelectList(users, "Id", "UserName");

            var isArabic = CultureInfo.CurrentUICulture.Name.StartsWith("ar");
            var ministries = await _context.Ministries.ToListAsync();

            ViewBag.Ministries = new SelectList(
                ministries.OrderBy(m => isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN),
                "Code",
                isArabic ? "MinistryDisplayName_AR" : "MinistryDisplayName_EN");

            // Only unreleased versions are assignment targets (a released version's
            // scope is frozen) — but keep the request's current version in the list
            // even if released, so re-saving the form doesn't silently detach it.
            var versions = await _context.PlatformVersions
                .Where(v => v.Status != VersionStatus.Released || v.Id == includeVersionId)
                .OrderByDescending(v => v.Id)
                .Select(v => new { v.Id, Label = v.VersionNumber + " — " + v.Title })
                .ToListAsync();

            ViewBag.Versions = new SelectList(versions, "Id", "Label", includeVersionId);

            // Named requester employees (not system accounts) for the "who wrote it" picker.
            var employees = await _context.RequestEmployees
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    e.Id,
                    Label = e.JobTitle != null ? e.Name + " — " + e.JobTitle : e.Name
                })
                .ToListAsync();

            ViewBag.Employees = new SelectList(employees, "Id", "Label");
        }
    }
}
