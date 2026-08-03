using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModels
{
    public class AuditChangeSummaryViewModel
    {
        public AuditLog Log { get; set; } = null!;

        // Max change lines shown before collapsing behind "+N more"
        public int MaxShown { get; set; } = 3;

        // Full mode: show every change line, and value lines for Create/Delete too
        public bool Full { get; set; } = false;

        // When true, "+N more" is a client-side expand toggle; otherwise it links to Details
        public bool AllowExpand { get; set; } = false;

        public int TruncateLength { get; set; } = 40;
    }
}
