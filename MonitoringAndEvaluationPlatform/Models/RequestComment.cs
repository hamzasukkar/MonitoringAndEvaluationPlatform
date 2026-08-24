using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A message on a request's testing/discussion thread. Always authored by the
    /// logged-in user (AuthorUserId) for accountability, optionally credited to a
    /// name-only employee (OnBehalfOfEmployeeId). Replies point at a parent comment.
    /// </summary>
    public class RequestComment : IHasTimestamps
    {
        [Key]
        public int Id { get; set; }

        public int RequestId { get; set; }
        public virtual Request Request { get; set; } = default!;

        /// <summary>Set when this comment is a reply to another (one level deep).</summary>
        public int? ParentCommentId { get; set; }
        public virtual RequestComment? ParentComment { get; set; }
        public virtual ICollection<RequestComment> Replies { get; set; } = new List<RequestComment>();

        /// <summary>The account that actually typed the comment.</summary>
        public string? AuthorUserId { get; set; }
        public virtual ApplicationUser? AuthorUser { get; set; }

        /// <summary>Optional: the named employee this comment is recorded on behalf of.</summary>
        public int? OnBehalfOfEmployeeId { get; set; }
        public virtual RequestEmployee? OnBehalfOfEmployee { get; set; }

        public CommentKind Kind { get; set; } = CommentKind.Comment;

        [StringLength(2000)]
        public string? Body { get; set; }

        public virtual ICollection<RequestCommentAttachment> Attachments { get; set; } = new List<RequestCommentAttachment>();

        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }
}
