namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ChartDataViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<double> RealData { get; set; } = new List<double>();
        public List<double> HistoricalData { get; set; } = new List<double>();
        public List<double> RequiredData { get; set; } = new List<double>();
    }
}
