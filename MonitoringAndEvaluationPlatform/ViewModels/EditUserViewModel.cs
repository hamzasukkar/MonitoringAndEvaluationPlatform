using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Ministry Name")]
        public string? MinistryName { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Lockout Enabled")]
        public bool LockoutEnabled { get; set; }

        [Display(Name = "Roles")]
        public List<string>? SelectedRoles { get; set; }

        public List<string> AvailableRoles { get; set; } = new List<string>();
        public List<Ministry> Ministries { get; set; } = new List<Ministry>();
    }
}
