using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// Authorized access to user-uploaded files.
    ///
    /// Attachments used to be served straight out of wwwroot/uploads, which UseStaticFiles
    /// handles BEFORE authentication - so every project, measure, request and goal attachment
    /// was readable by anyone who had (or guessed) the URL, with no ministry scoping at all.
    /// Uploads now live outside the web root and are only reachable through these actions,
    /// which resolve the owning record and apply the same ministry scope as the rest of the app.
    /// </summary>
    [Authorize]
    public class FilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUploadValidationService _uploadValidation;
        private readonly IMinistryScopeService _scope;

        public FilesController(
            ApplicationDbContext context,
            IUploadValidationService uploadValidation,
            IMinistryScopeService scope)
        {
            _context = context;
            _uploadValidation = uploadValidation;
            _scope = scope;
        }

        /// <summary>Measure attachment. Scoped via Measure -> ProjectPhase -> Project.</summary>
        [HttpGet]
        public async Task<IActionResult> Measure(int id, bool download = false)
        {
            var file = await _context.MeasureFiles.FirstOrDefaultAsync(f => f.Id == id);
            if (file == null) return NotFound();

            if (!await _scope.MeasureBelongsToScopeAsync(file.MeasureCode)) return Forbid();

            return Serve(file.FilePath, file.FileName, download);
        }

        /// <summary>Framework goal attachment. Scoped via FrameworkGoal -> Framework.</summary>
        [HttpGet]
        public async Task<IActionResult> FrameworkGoal(int id, bool download = false)
        {
            var file = await _context.FrameworkGoalFiles
                .Include(f => f.FrameworkGoal)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (file == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await _scope.GetScopeAsync();
            if (!isAdmin)
            {
                if (scopedMinistryCode is null) return Forbid();

                var belongs = await _context.FrameworkGoals
                    .Where(g => g.ID == file.FrameworkGoalID)
                    .AnyAsync(g => g.Framework.MinistryCode == scopedMinistryCode);
                if (!belongs) return Forbid();
            }

            return Serve(file.FilePath, file.FileName, download);
        }

        /// <summary>
        /// Request attachment. Requests are not ministry-scoped; a user may read the files on
        /// a request they submitted, one assigned to them, or any request if they can manage
        /// requests.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Request(int id, bool download = false)
        {
            var file = await _context.RequestFiles
                .Include(f => f.Request)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (file == null) return NotFound();

            var canManage = (await HttpContext.RequestServices
                .GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(User, Models.Permissions.ManageRequests)).Succeeded;

            if (!canManage)
            {
                var userId = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isParticipant = file.Request != null
                    && (file.Request.SubmittedByUserId == userId || file.Request.AssignedToUserId == userId);
                if (!isParticipant) return Forbid();
            }

            return Serve(file.FilePath, file.FileName, download);
        }

        /// <summary>
        /// Streams a stored file. The path is resolved through the upload service, which
        /// containment-checks it against the uploads root, so a poisoned database value cannot
        /// read arbitrary files off disk.
        /// </summary>
        private IActionResult Serve(string storedPath, string displayName, bool forceDownload)
        {
            var fullPath = _uploadValidation.ResolveStoredPath(storedPath);
            if (fullPath == null || !System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var extension = Path.GetExtension(fullPath).ToLowerInvariant();

            // Inline only for formats that cannot carry script; everything else downloads.
            var inlineSafe = !forceDownload
                && extension is ".pdf" or ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp";

            var contentDisposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue(
                inlineSafe ? "inline" : "attachment");
            contentDisposition.SetHttpFileName(displayName);
            Response.Headers.ContentDisposition = contentDisposition.ToString();
            Response.Headers.XContentTypeOptions = "nosniff";

            return PhysicalFile(fullPath, ContentTypeFor(extension));
        }

        private static string ContentTypeFor(string extension) => extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }
}
