using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class IndicatorDetailsViewModel
    {
        public Indicator Indicator { get; set; }
        public List<(string Name, string Type, int Code)> Hierarchy { get; set; }
    }

}
