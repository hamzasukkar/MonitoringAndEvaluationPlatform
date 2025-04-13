namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class FrameworkProgressItem
    {
        public string FrameworkName { get; set; }
        public double AchievementRate { get; set; } // Normalized 0–100
        public int TotalIndicators { get; set; }
        public double TotalTarget { get; set; }
        public double TotalAchieved { get; set; }
    }

    public class FrameworkProgressViewModel
    {
        public List<FrameworkProgressItem> Frameworks { get; set; } = new();
    }
}
