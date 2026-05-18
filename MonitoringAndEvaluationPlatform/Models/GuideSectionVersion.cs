using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A point-in-time snapshot of a <see cref="GuideSection"/>. One row is
    /// written every time an admin confirms a change, storing the content
    /// that was replaced so it can be compared and restored.
    /// </summary>
    public class GuideSectionVersion
    {
        [Key]
        public int Id { get; set; }

        public int GuideSectionId { get; set; }
        public GuideSection? GuideSection { get; set; }

        /// <summary>The HTML that this version held (the "old" content).</summary>
        [Required]
        public string ContentHtml { get; set; } = string.Empty;

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(256)]
        public string? SavedBy { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
