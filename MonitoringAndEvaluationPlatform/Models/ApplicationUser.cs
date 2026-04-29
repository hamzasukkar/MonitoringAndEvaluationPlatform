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
    }
}
