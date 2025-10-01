namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProjectProgressViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public double CompletionRate { get; set; }

        public int TotalIndicators { get; set; }
        public double TotalTarget { get; set; }
        public double TotalAchieved { get; set; }
    }
}
