namespace MonitoringAndEvaluationPlatform.Dtos
{
    public class DashboardSummaryDto
    {
        public int TotalFrameworks { get; set; }
        public int TotalIndicators { get; set; }
        public int TotalProjects { get; set; }
        public int TotalGovernorates { get; set; }
        public int TotalMinistries { get; set; }
        public int TotalOutcomes { get; set; }
        public int TotalOutputs { get; set; }
        public int TotalSubOutputs { get; set; }
    }

    public class FrameworkPerformanceDto
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public double IndicatorsPerformance { get; set; }
        public int IndicatorCount { get; set; }
        public List<ProjectPerformanceDto> Projects { get; set; } = new();
    }

    public class ProjectPerformanceDto
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public double Performance { get; set; }
    }

    public class OutcomeProgressDto
    {
        public string OutcomeName { get; set; } = string.Empty;
        public int TotalIndicators { get; set; }
        public double TotalTarget { get; set; }
        public double TotalAchieved { get; set; }
        public double AchievementRate { get; set; }
    }

    public class ProjectProgressDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public double CompletionRate { get; set; }
        public int TotalIndicators { get; set; }
        public double TotalTarget { get; set; }
        public double TotalAchieved { get; set; }
    }

    public class IndicatorTrendDto
    {
        public List<TrendPointDto> Real { get; set; } = new();
        public List<TrendPointDto> Target { get; set; } = new();
    }

    public class TrendPointDto
    {
        public string Date { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}
