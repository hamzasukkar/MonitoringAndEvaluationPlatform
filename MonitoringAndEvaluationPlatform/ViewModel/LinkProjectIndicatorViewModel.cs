using Microsoft.AspNetCore.Mvc.Rendering;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class LinkProjectIndicatorViewModel
    {
        public int SelectedProjectId { get; set; }
        public int SelectedFrameworkCode { get; set; }
        public int SelectedOutcomeCode { get; set; }
        public int SelectedOutputCode { get; set; }
        public int SelectedSubOutputCode { get; set; }
        public List<int> SelectedIndicatorCodes { get; set; } = new();
        public List<Indicator> LinkedIndicators { get; set; } = new();
        public List<SelectListItem> Projects { get; set; } = new();
        public List<SelectListItem> Frameworks { get; set; } = new();
        public List<SelectListItem> Outcomes { get; set; } = new();
        public List<SelectListItem> Outputs { get; set; } = new();
        public List<SelectListItem> SubOutputs { get; set; } = new();
        public List<SelectListItem> Indicators { get; set; } = new();
    }
}
