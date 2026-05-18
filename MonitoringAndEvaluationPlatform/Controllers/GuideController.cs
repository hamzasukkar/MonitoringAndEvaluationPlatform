using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// Admin-only editing of the User Guide. Public rendering of the guide
    /// stays in HomeController/UserGuide; this controller handles the edit
    /// panel: save (with versioning), history, and restore.
    /// </summary>
    [Authorize(Roles = UserRoles.SystemAdministrator)]
    public class GuideController : Controller
    {
        private readonly IGuideService _guideService;

        public GuideController(IGuideService guideService)
        {
            _guideService = guideService;
        }

        public class SaveSectionRequest
        {
            public string SectionKey { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string NewHtml { get; set; } = string.Empty;
            public string? OriginalHtml { get; set; }
            public string? Note { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSection([FromBody] SaveSectionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SectionKey)
                || string.IsNullOrWhiteSpace(request.NewHtml))
            {
                return BadRequest(new { ok = false, message = "بيانات غير صالحة." });
            }

            var previous = await _guideService.SaveSectionAsync(
                request.SectionKey.Trim(),
                request.Title?.Trim() ?? string.Empty,
                request.NewHtml,
                request.OriginalHtml,
                User?.Identity?.Name,
                request.Note);

            return Json(new { ok = true, previousHtml = previous });
        }

        [HttpGet]
        public async Task<IActionResult> Section(string sectionKey)
        {
            if (string.IsNullOrWhiteSpace(sectionKey))
                return BadRequest();

            var section = await _guideService.GetSectionAsync(sectionKey);
            return Json(new
            {
                exists = section != null,
                contentHtml = section?.ContentHtml,
                updatedAt = section?.UpdatedAt,
                updatedBy = section?.UpdatedBy
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(string slotFileName, IFormFile file)
        {
            var (ok, error, url) = await _guideService.SaveGuideImageAsync(slotFileName, file);
            if (!ok)
                return BadRequest(new { ok = false, message = error });

            return Json(new { ok = true, url });
        }

        [HttpGet]
        public async Task<IActionResult> History(string? sectionKey)
        {
            ViewBag.Sections = await _guideService.GetAllSectionsAsync();
            ViewBag.SelectedKey = sectionKey;

            if (!string.IsNullOrWhiteSpace(sectionKey))
            {
                ViewBag.CurrentSection = await _guideService.GetSectionAsync(sectionKey);
                ViewBag.Versions = await _guideService.GetVersionsAsync(sectionKey);
            }
            else
            {
                ViewBag.Versions = new List<GuideSectionVersion>();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int versionId, string? sectionKey)
        {
            var ok = await _guideService.RestoreVersionAsync(versionId, User?.Identity?.Name);
            if (!ok)
                TempData["GuideError"] = "تعذّر استعادة هذه النسخة.";
            else
                TempData["GuideSuccess"] = "تمت استعادة النسخة المختارة بنجاح.";

            return RedirectToAction(nameof(History), new { sectionKey });
        }
    }
}
