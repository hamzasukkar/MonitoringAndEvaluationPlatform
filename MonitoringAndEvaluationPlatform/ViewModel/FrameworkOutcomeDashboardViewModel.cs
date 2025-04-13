using Microsoft.AspNetCore.Mvc.Rendering;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class FrameworkOutcomeDashboardViewModel
    {
        public List<FrameworkProgressItem> Frameworks { get; set; } = new();
        public List<OutcomeProgressItem> Outcomes { get; set; } = new();
        public int? SelectedFrameworkCode { get; set; }
        public List<SelectListItem> FrameworkOptions { get; set; } = new();
    }

}
