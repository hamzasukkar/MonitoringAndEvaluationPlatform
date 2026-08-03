using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A formal "I tried this request and it works" tick. The tester is the named
    /// employee (RequestEmployeeId, required) — that employee is the identity of the
    /// tick, and a unique index on (RequestId, RequestEmployeeId) keeps it to one per
    /// person per request. RecordedByUserId keeps the account that actually entered it,
    /// mirroring RequestComment.AuthorUserId. Distinct from a CommentKind.TestPassed
    /// comment, which stays as free-form discussion.
    /// </summary>
    public class RequestTest : IHasTimestamps
    {
        [Key]
        public int Id { get; set; }

        public int RequestId { get; set; }
        public virtual Request Request { get; set; } = default!;

        /// <summary>The named employee who did the testing. Required — it is the key of the tick.</summary>
        [Display(Name = "Tested By")]
        public int RequestEmployeeId { get; set; }
        public virtual RequestEmployee RequestEmployee { get; set; } = default!;

        /// <summary>The account that entered the tick; may differ from the tester.</summary>
        public string? RecordedByUserId { get; set; }
        public virtual ApplicationUser? RecordedByUser { get; set; }

        /// <summary>Optional short remark, e.g. "tested on the staging server".</summary>
        [Display(Name = "Note")]
        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }
}
