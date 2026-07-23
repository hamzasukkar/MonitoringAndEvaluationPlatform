using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A platform release (Jira-style fix version). Requests are assigned to the
    /// version they will ship in; released versions feed the "What's New" page.
    /// Named PlatformVersion (not Version) to avoid clashing with System.Version.
    /// </summary>
    public class PlatformVersion : IHasTimestamps
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Free-form scheme chosen by the admin, e.g. "1.2.0". Unique.</summary>
        [Required]
        [StringLength(20)]
        [Display(Name = "Version Number")]
        public string VersionNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(2000)]
        public string? Description { get; set; }

        [Display(Name = "Status")]
        public VersionStatus Status { get; set; } = VersionStatus.Planned;

        [Display(Name = "Planned Date")]
        [DataType(DataType.Date)]
        public DateTime? PlannedDate { get; set; }

        [Display(Name = "Release Date")]
        [DataType(DataType.Date)]
        public DateTime? ReleaseDate { get; set; }

        public ICollection<Request> Requests { get; set; } = new List<Request>();

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime LastModifiedAt { get; set; }
    }
}
