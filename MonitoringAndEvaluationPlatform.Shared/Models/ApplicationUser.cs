using Microsoft.AspNetCore.Identity;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? MinistryName { get; set; }
    }
}
