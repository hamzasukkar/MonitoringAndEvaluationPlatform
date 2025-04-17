namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class OutcomeProgressItem
    {
        public string OutcomeName { get; set; }
        public double AchievementRate { get; set; }
        public int TotalIndicators { get; set; }
        public double TotalTarget { get; set; }
        public double TotalAchieved { get; set; }
    }

    public class OutcomeProgressViewModel
    {
        public List<OutcomeProgressItem> Outcomes { get; set; } = new();
    }

}
