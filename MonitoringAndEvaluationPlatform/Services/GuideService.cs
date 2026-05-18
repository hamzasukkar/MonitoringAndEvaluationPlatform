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
                        ContentHtml = originalHtml,
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

            section.ContentHtml = version.ContentHtml;
            section.UpdatedAt = DateTime.UtcNow;
            section.UpdatedBy = user;
            await _context.SaveChangesAsync();

            await WriteDiskBackupAsync(section.SectionKey, section.ContentHtml);
            return true;
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
