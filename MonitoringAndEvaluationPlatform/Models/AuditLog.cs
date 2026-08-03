using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Entity Name")]
        public string EntityName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Entity ID")]
        public string EntityId { get; set; } = string.Empty;

        [StringLength(256)]
        [Display(Name = "Entity Display Name")]
        public string? EntityDisplayName { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Action")]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete

        [Display(Name = "Old Values")]
        public string? OldValues { get; set; } // JSON

        [Display(Name = "New Values")]
        public string? NewValues { get; set; } // JSON

        [Display(Name = "Changed Columns")]
        public string? ChangedColumns { get; set; } // Comma-separated list

        [StringLength(450)]
        [Display(Name = "User ID")]
        public string? UserId { get; set; }

        [StringLength(256)]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [Required]
        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        [StringLength(512)]
        [Display(Name = "User Agent")]
        public string? UserAgent { get; set; }
    }
}
