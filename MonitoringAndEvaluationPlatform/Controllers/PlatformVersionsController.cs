using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class PlatformVersionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<PlatformVersionsController> _localizer;

        public PlatformVersionsController(
            ApplicationDbContext context,
            IStringLocalizer<PlatformVersionsController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // ---------------------------------------------------------------- Management

        // GET: PlatformVersions
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> Index()
        {
            // Unreleased first (planned/in-development are the working set),
            // then released newest-first.
            var versions = await _context.PlatformVersions
                .Include(v => v.Requests)
                .OrderBy(v => v.Status == VersionStatus.Released ? 1 : 0)
                .ThenByDescending(v => v.ReleaseDate)
                .ThenByDescending(v => v.Id)
                .ToListAsync();

            return View(versions);
        }

        // GET: PlatformVersions/Create
        [Permission(Permissions.ManageRequests)]
        public IActionResult Create()
        {
            return View(new PlatformVersion());
        }

        // POST: PlatformVersions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> Create(PlatformVersion version)
        {
            await ValidateUniqueNumberAsync(version);

            if (!ModelState.IsValid)
            {
                return View(version);
            }

            // ReleaseDate is owned by the Release action, not the form.
            version.ReleaseDate = null;
            if (version.Status == VersionStatus.Released)
            {
                version.Status = VersionStatus.Planned;
            }

            _context.PlatformVersions.Add(version);
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(string.Format(
                _localizer["Version '{0}' has been created."].Value, version.VersionNumber));

            return RedirectToAction(nameof(Details), new { id = version.Id });
        }

        // GET: PlatformVersions/Details/5
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var version = await _context.PlatformVersions
                .Include(v => v.Requests)
                    .ThenInclude(r => r.AssignedToUser)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null) return NotFound();

            return View(version);
        }

        // GET: PlatformVersions/Edit/5
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var version = await _context.PlatformVersions.FindAsync(id);
            if (version == null) return NotFound();

            return View(version);
        }

        // POST: PlatformVersions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> Edit(int id, PlatformVersion version)
        {
            if (id != version.Id) return NotFound();

            await ValidateUniqueNumberAsync(version);

            if (!ModelState.IsValid)
            {
                return View(version);
            }

            var existing = await _context.PlatformVersions.FirstOrDefaultAsync(v => v.Id == id);
            if (existing == null) return NotFound();

            existing.VersionNumber = version.VersionNumber;
            existing.Title = version.Title;
            existing.Description = version.Description;
            existing.PlannedDate = version.PlannedDate;

            // Status moves between Planned/InDevelopment here; Released only via Release.
            if (existing.Status != VersionStatus.Released
                && version.Status != VersionStatus.Released)
            {
                existing.Status = version.Status;
            }

            await _context.SaveChangesAsync();

            this.SetSuccessMessage(string.Format(
                _localizer["Version '{0}' has been updated."].Value, existing.VersionNumber));

            return RedirectToAction(nameof(Details), new { id = existing.Id });
        }

        // POST: PlatformVersions/Release/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ManageRequests)]
        public async Task<IActionResult> Release(int id)
        {
            var version = await _context.PlatformVersions
                .Include(v => v.Requests)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null) return NotFound();

            if (version.Status == VersionStatus.Released)
            {
                this.SetWarningMessage(_localizer["This version is already released."].Value);
                return RedirectToAction(nameof(Details), new { id });
            }

            version.Status = VersionStatus.Released;
            version.ReleaseDate = DateTime.Today;
            await _context.SaveChangesAsync();

            var incomplete = version.Requests.Count(r =>
                r.Status != RequestStatus.Completed && r.Status != RequestStatus.Cancelled);

            if (incomplete > 0)
            {
                this.SetWarningMessage(string.Format(
                    _localizer["Version '{0}' released. {1} linked request(s) are not completed and will not appear in What's New."].Value,
                    version.VersionNumber, incomplete));
            }
            else
            {
                this.SetSuccessMessage(string.Format(
                    _localizer["Version '{0}' has been released."].Value, version.VersionNumber));
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: PlatformVersions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.DeleteRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            var version = await _context.PlatformVersions.FindAsync(id);
            if (version == null) return NotFound();

            // Linked requests are detached automatically (FK is SetNull).
            _context.PlatformVersions.Remove(version);
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(string.Format(
                _localizer["Version '{0}' has been deleted."].Value, version.VersionNumber));

            return RedirectToAction(nameof(Index));
        }

        // ---------------------------------------------------------------- Public

        // GET: PlatformVersions/WhatsNew — release notes for every signed-in user.
        [Authorize]
        public async Task<IActionResult> WhatsNew()
        {
            var released = await _context.PlatformVersions
                .Where(v => v.Status == VersionStatus.Released)
                .Include(v => v.Requests.Where(r => r.Status == RequestStatus.Completed))
                .OrderByDescending(v => v.ReleaseDate)
                .ThenByDescending(v => v.Id)
                .ToListAsync();

            return View(released);
        }

        // ---------------------------------------------------------------- Helpers

        private async Task ValidateUniqueNumberAsync(PlatformVersion version)
        {
            var duplicate = await _context.PlatformVersions
                .AnyAsync(v => v.VersionNumber == version.VersionNumber && v.Id != version.Id);

            if (duplicate)
            {
                ModelState.AddModelError(nameof(PlatformVersion.VersionNumber),
                    _localizer["A version with this number already exists."].Value);
            }
        }
    }
}
