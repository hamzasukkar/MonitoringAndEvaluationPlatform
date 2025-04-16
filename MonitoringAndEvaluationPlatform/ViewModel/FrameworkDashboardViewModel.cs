using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class FrameworkDashboardViewModel
    {
        public List<Framework> Frameworks { get; set; }
        public List<Ministry> Ministries { get; set; }
        public List<int> SelectedMinistryIds { get; set; } = new List<int>();
    }
}
