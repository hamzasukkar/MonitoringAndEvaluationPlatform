using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public interface IGuideService
    {
        /// <summary>
        /// Returns saved overrides keyed by SectionKey. Sections that were
        /// never edited are absent (the static Razor markup is the default).
        /// </summary>
        Task<Dictionary<string, string>> GetOverridesAsync();

        /// <summary>
        /// Stores a new version: pushes the current content into history,
        /// upserts the section, and writes a disk backup. On the first edit
        /// of a section, <paramref name="originalHtml"/> (the default content
        /// shown before editing) is captured as a restorable baseline version.
        /// Returns the previous HTML (or null if this section had no override yet).
        /// </summary>
        Task<string?> SaveSectionAsync(string sectionKey, string title, string newHtml, string? originalHtml, string? user, string? note);

        Task<GuideSection?> GetSectionAsync(string sectionKey);

        Task<List<GuideSectionVersion>> GetVersionsAsync(string sectionKey);

        Task<List<GuideSection>> GetAllSectionsAsync();

        /// <summary>Restores a historical version as the current content.</summary>
        Task<bool> RestoreVersionAsync(int versionId, string? user);
    }
}
