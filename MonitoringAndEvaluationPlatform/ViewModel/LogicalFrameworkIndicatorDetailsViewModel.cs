using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class LogicalFrameworkIndicatorDetailsViewModel
    {
        public LogicalFrameworkIndicator LogicalFrameworkIndicator { get; set; }
        public List<LogicalMeasure> logicalMeasures { get; set; }
        public ChartDataViewModel ChartDataViewModel { get; set; }

        public LogicalMeasure NewLogicalMeasure { get; set; } = new LogicalMeasure();
    }

}
