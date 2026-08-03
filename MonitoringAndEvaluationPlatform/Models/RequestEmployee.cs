using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A named person who originates requests but is NOT a system account.
    /// Mirrors the ProjectManager lookup pattern (name/phone/email) plus a job
    /// title. Referenced by Request.RequestEmployeeId so requests can be filtered
    /// by who wrote them without granting those people logins.
    /// </summary>
    public class RequestEmployee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(200)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        public ICollection<Request> Requests { get; set; } = new List<Request>();

        /// <summary>Test sign-offs this employee recorded. Blocks deletion while non-empty.</summary>
        public ICollection<RequestTest> Tests { get; set; } = new List<RequestTest>();
    }
}
