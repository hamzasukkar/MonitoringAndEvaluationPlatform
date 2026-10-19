using Ganss.Xss;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public class GuideService : IGuideService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GuideService> _logger;

        /// <summary>
        /// Allow-list sanitizer for administrator-authored guide HTML. Stateless and
        /// thread-safe once configured, so a single shared instance is fine.
        /// </summary>
        private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

        private static HtmlSanitizer CreateSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            // The editor needs inline styling and Bootstrap/FontAwesome classes.
            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedAttributes.Add("style");
            sanitizer.AllowedAttributes.Add("id");

            // Never allow framing or script-bearing elements, whatever the defaults say.
            sanitizer.AllowedTags.Remove("iframe");
            sanitizer.AllowedTags.Remove("object");
            sanitizer.AllowedTags.Remove("embed");
            sanitizer.AllowedTags.Remove("form");

            return sanitizer;
        }

        private static string SanitizeHtml(string? html) =>
            string.IsNullOrWhiteSpace(html) ? string.Empty : Sanitizer.Sanitize(html);

        public GuideService(ApplicationDbContext context, IWebHostEnvironment env, ILogger<GuideService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        public async Task<Dictionary<string, string>> GetOverridesAsync()
        {
            return await _context.GuideSections
                .AsNoTracking()
                .ToDictionaryAsync(s => s.SectionKey, s => s.ContentHtml);
        }

        public Task<GuideSection?> GetSectionAsync(string sectionKey) =>
            _context.GuideSections
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SectionKey == sectionKey);

        public Task<List<GuideSection>> GetAllSectionsAsync() =>
            _context.GuideSections
                .AsNoTracking()
                .OrderBy(s => s.SectionKey)
                .ToListAsync();

        public async Task<List<GuideSectionVersion>> GetVersionsAsync(string sectionKey)
        {
            var section = await _context.GuideSections
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SectionKey == sectionKey);

            if (section == null)
                return new List<GuideSectionVersion>();

            return await _context.GuideSectionVersions
                .AsNoTracking()
                .Where(v => v.GuideSectionId == section.Id)
                .OrderByDescending(v => v.SavedAt)
                .ToListAsync();
        }

        public async Task<string?> SaveSectionAsync(string sectionKey, string title, string newHtml, string? originalHtml, string? user, string? note)
        {
            // Sanitize on write, not on render: guide content is emitted raw by
            // Views/Guide/History.cshtml and re-serialized into the user guide, so cleaning it
            // here covers every read path with one call and leaves nothing dangerous in the
            // database. Guide editing is administrator-only, so this is defence in depth
            // against an administrator account being used to plant persistent script.
            newHtml = SanitizeHtml(newHtml);

            var section = await _context.GuideSections
                .FirstOrDefaultAsync(s => s.SectionKey == sectionKey);

            string? previousHtml = null;

            if (section == null)
            {
                section = new GuideSection
                {
                    SectionKey = sectionKey,
                    Title = title,
                    ContentHtml = newHtml,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = user
                };
                _context.GuideSections.Add(section);
                await _context.SaveChangesAsync();

                // First edit: keep the original default content as a
                // restorable baseline so it can always be reverted to.
                if (!string.IsNullOrWhiteSpace(originalHtml)
                    && !string.Equals(originalHtml, newHtml, StringComparison.Ordinal))
                {
                    _context.GuideSectionVersions.Add(new GuideSectionVersion
                    {
                        GuideSectionId = section.Id,
                        ContentHtml = SanitizeHtml(originalHtml),
                        SavedAt = DateTime.UtcNow,
                        SavedBy = user,
                        Note = "النسخة الأصلية قبل أول تعديل"
                    });
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                previousHtml = section.ContentHtml;

                // Snapshot the content being replaced so it can be compared/restored.
                _context.GuideSectionVersions.Add(new GuideSectionVersion
                {
                    GuideSectionId = section.Id,
                    ContentHtml = previousHtml,
                    SavedAt = DateTime.UtcNow,
                    SavedBy = user,
                    Note = note
                });

                section.ContentHtml = newHtml;
                section.Title = string.IsNullOrWhiteSpace(title) ? section.Title : title;
                section.UpdatedAt = DateTime.UtcNow;
                section.UpdatedBy = user;
                await _context.SaveChangesAsync();
            }

            await WriteDiskBackupAsync(sectionKey, newHtml);
            return previousHtml;
        }

        public async Task<bool> RestoreVersionAsync(int versionId, string? user)
        {
            var version = await _context.GuideSectionVersions
                .FirstOrDefaultAsync(v => v.Id == versionId);
            if (version == null)
                return false;

            var section = await _context.GuideSections
                .FirstOrDefaultAsync(s => s.Id == version.GuideSectionId);
            if (section == null)
                return false;

            // Restoring is itself a change, so snapshot current content first.
            _context.GuideSectionVersions.Add(new GuideSectionVersion
            {
                GuideSectionId = section.Id,
                ContentHtml = section.ContentHtml,
                SavedAt = DateTime.UtcNow,
                SavedBy = user,
                Note = $"تم الاستبدال باستعادة نسخة بتاريخ {version.SavedAt:yyyy-MM-dd HH:mm}"
            });

            // Re-sanitize on restore: stored versions may predate sanitization on write.
            section.ContentHtml = SanitizeHtml(version.ContentHtml);
            section.UpdatedAt = DateTime.UtcNow;
            section.UpdatedBy = user;
            await _context.SaveChangesAsync();

            await WriteDiskBackupAsync(section.SectionKey, section.ContentHtml);
            return true;
        }

        private static readonly string[] AllowedImageExtensions = { ".png", ".jpg", ".jpeg", ".webp" };
        private const long MaxImageBytes = 5 * 1024 * 1024;

        public async Task<(bool Ok, string? Error, string? Url)> SaveGuideImageAsync(string slotFileName, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "لم يتم اختيار ملف.", null);

            if (file.Length > MaxImageBytes)
                return (false, "حجم الصورة يتجاوز 5 ميغابايت.", null);

            // Only the base name is trusted - never a caller-supplied path.
            var name = Path.GetFileName(slotFileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(name))
                return (false, "اسم الصورة غير صالح.", null);

            var ext = Path.GetExtension(name).ToLowerInvariant();
            if (Array.IndexOf(AllowedImageExtensions, ext) < 0)
                return (false, "صيغة غير مدعومة. المسموح: PNG أو JPG أو WebP.", null);

            // The bytes themselves must look like one of the allowed formats.
            if (!await HasValidImageSignatureAsync(file))
                return (false, "الملف ليس صورة PNG/JPG/WebP صالحة.", null);

            var dir = Path.Combine(_env.WebRootPath, "images", "guide");
            Directory.CreateDirectory(dir);

            var fullPath = Path.Combine(dir, name);

            // Defense in depth: the resolved path must stay inside images/guide.
            var dirFull = Path.GetFullPath(dir + Path.DirectorySeparatorChar);
            if (!Path.GetFullPath(fullPath).StartsWith(dirFull, StringComparison.OrdinalIgnoreCase))
                return (false, "مسار غير مسموح.", null);

            try
            {
                using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save guide image {Name}", name);
                return (false, "تعذّر حفظ الصورة على الخادم.", null);
            }

            return (true, null, $"/images/guide/{name}");
        }

        private static async Task<bool> HasValidImageSignatureAsync(IFormFile file)
        {
            var head = new byte[12];
            await using (var s = file.OpenReadStream())
            {
                var read = await s.ReadAsync(head.AsMemory(0, head.Length));
                if (read < 12) return false;
            }

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47)
                return true;

            // JPEG: FF D8 FF
            if (head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
                return true;

            // WebP: "RIFF" .... "WEBP"
            if (head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F'
                && head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P')
                return true;

            return false;
        }

        /// <summary>
        /// Mirrors the saved content to wwwroot/docs/user-guide so there is a
        /// plain-file copy on disk in addition to the database row.
        /// </summary>
        private async Task WriteDiskBackupAsync(string sectionKey, string html)
        {
            try
            {
                var dir = Path.Combine(_env.WebRootPath, "docs", "user-guide");
                Directory.CreateDirectory(dir);

                var safeKey = string.Concat(sectionKey.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
                var path = Path.Combine(dir, $"{safeKey}.ar.html");
                await File.WriteAllTextAsync(path, html, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // A backup failure must not block saving to the database.
                _logger.LogWarning(ex, "Failed to write guide disk backup for section {SectionKey}", sectionKey);
            }
        }
    }
}
