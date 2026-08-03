using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Request : IHasTimestamps
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Generated once on creation and stored (REQ-yyyy-000). Never recomputed,
        /// so sorting or deleting rows cannot renumber existing requests.
        /// </summary>
        [Display(Name = "Request Number")]
        [StringLength(20)]
        public string RequestNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Request Date")]
        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; } = DateTime.Today;

        [Display(Name = "Meeting Date")]
        [DataType(DataType.Date)]
        public DateTime? MeetingDate { get; set; }

        [Display(Name = "Requesting Party")]
        [StringLength(200)]
        public string? RequestingParty { get; set; }

        [Display(Name = "Submitted By")]
        public string? SubmittedByUserId { get; set; }
        public virtual ApplicationUser? SubmittedByUser { get; set; }

        /// <summary>The named employee who wrote the request (not a system account).</summary>
        [Display(Name = "Request Employee")]
        public int? RequestEmployeeId { get; set; }
        public virtual RequestEmployee? RequestEmployee { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(2000)]
        public string? Description { get; set; }

        [Display(Name = "Category")]
        public RequestCategory Category { get; set; } = RequestCategory.NewRequest;

        [Display(Name = "Priority")]
        public RequestPriority Priority { get; set; } = RequestPriority.Medium;

        [Display(Name = "Assigned To")]
        public string? AssignedToUserId { get; set; }
        public virtual ApplicationUser? AssignedToUser { get; set; }

        [Display(Name = "Status")]
        public RequestStatus Status { get; set; } = RequestStatus.New;

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Actual Completion Date")]
        [DataType(DataType.Date)]
        [DateRangeValidation(nameof(StartDate))]
        public DateTime? ActualCompletionDate { get; set; }

        [Display(Name = "Completion Percentage")]
        [Range(0, 100)]
        public int CompletionPercentage { get; set; }

        [Display(Name = "Notes")]
        [StringLength(2000)]
        public string? Notes { get; set; }

        [Display(Name = "Ministry")]
        public int? MinistryCode { get; set; }
        public virtual Ministry? Ministry { get; set; }

        [Display(Name = "Target Version")]
        public int? PlatformVersionId { get; set; }
        public virtual PlatformVersion? PlatformVersion { get; set; }

        public ICollection<RequestFile> Files { get; set; } = new List<RequestFile>();

        public ICollection<RequestComment> Comments { get; set; } = new List<RequestComment>();

        /// <summary>Formal test sign-offs — one per named employee who verified the request.</summary>
        public ICollection<RequestTest> Tests { get; set; } = new List<RequestTest>();

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime LastModifiedAt { get; set; }
    }
}
