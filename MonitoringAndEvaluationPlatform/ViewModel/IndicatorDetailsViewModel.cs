using MonitoringAndEvaluationPlatform.Models;
using System.Collections.Generic;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class IndicatorDetailsViewModel
    {
        public Indicator Indicator { get; set; }
        public List<(string Name, string Type, int Code)> Hierarchy { get; set; }
        public List<Measure> Measures { get; set; }
        public ChartDataViewModel ChartDataViewModel { get; set; }
    }

}
