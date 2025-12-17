using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Ministry Name")]
        public string? MinistryName { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; } = true;

        [Display(Name = "Roles")]
        public List<string>? SelectedRoles { get; set; }

        public List<string> AvailableRoles { get; set; } = new List<string>();
        public List<Ministry> Ministries { get; set; } = new List<Ministry>();
    }
}
