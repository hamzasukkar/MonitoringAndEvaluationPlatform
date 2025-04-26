using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class LogicalFrameworkIndicatorDetailsViewModel
    {
        public LogicalFrameworkIndicator LogicalFrameworkIndicator { get; set; }
        public List<Measure> Measures { get; set; }
        public ChartDataViewModel ChartDataViewModel { get; set; }
    }
}
