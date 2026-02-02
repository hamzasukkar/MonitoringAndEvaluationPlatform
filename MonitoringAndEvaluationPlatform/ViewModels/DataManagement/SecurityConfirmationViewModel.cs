using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.ViewModels.DataManagement
{
    public class SecurityConfirmationViewModel
    {
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmation phrase is required")]
        public string ConfirmationPhrase { get; set; } = string.Empty;

        public string ExpectedPhrase { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;
    }
}
