using Microsoft.AspNetCore.Mvc.Rendering;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    // Backs the Outcomes/Move confirmation page. Moving an outcome is a single FK
    // change (Outcome.FrameworkCode), but it drags the whole Output -> SubOutput ->
    // Indicator subtree with it, so the page shows the impact before committing.
    public class MoveOutcomeViewModel
    {
        public int OutcomeCode { get; set; }
        public string OutcomeName { get; set; } = string.Empty;

        public int SourceFrameworkCode { get; set; }
        public string SourceFrameworkName { get; set; } = string.Empty;
        public string? SourceMinistryName { get; set; }

        public int? DestinationFrameworkCode { get; set; }

        // Impact preview: what travels with the outcome.
        public int OutputCount { get; set; }
        public int SubOutputCount { get; set; }
        public int IndicatorCount { get; set; }
        public int LinkedProjectCount { get; set; }

        public List<SelectListItem> AvailableFrameworks { get; set; } = new();

        // Framework code -> owning ministry display name, so the view can warn when the
        // selected destination belongs to a different ministry than the source.
        public Dictionary<int, string> FrameworkMinistries { get; set; } = new();
    }
}
