using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? MinistryName { get; set; }

        public int? MinistryCode { get; set; }

        [ForeignKey(nameof(MinistryCode))]
        public Ministry? Ministry { get; set; }

        /// <summary>
        /// Forces the user to set a new password before they can use the application.
        /// Set on every server-created account so administrator-chosen or seeded passwords
        /// are never usable long-term. Enforced by <see cref="Infrastructure.PasswordChangeMiddleware"/>.
        /// </summary>
        public bool MustChangePassword { get; set; }

        /// <summary>
        /// When the password was last changed. Null for accounts that have never changed it.
        /// </summary>
        public DateTime? PasswordChangedAt { get; set; }
    }
}
