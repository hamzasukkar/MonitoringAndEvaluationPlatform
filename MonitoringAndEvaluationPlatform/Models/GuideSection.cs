using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// An admin-editable override for one section of the User Guide.
    /// Keyed by the section's HTML id in Views/Home/UserGuide.cshtml
    /// (e.g. "introduction", "dashboard"). If no row exists for a section,
    /// the static Razor markup is shown as the default.
    /// </summary>
    public class GuideSection
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The section's HTML id (also its TOC anchor).</summary>
        [Required]
        [MaxLength(100)]
        public string SectionKey { get; set; } = string.Empty;

        /// <summary>Human-readable label shown in the history screen.</summary>
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Current (Arabic) HTML for the whole section.</summary>
        [Required]
        public string ContentHtml { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(256)]
        public string? UpdatedBy { get; set; }

        public ICollection<GuideSectionVersion> Versions { get; set; } = new List<GuideSectionVersion>();
    }
}
