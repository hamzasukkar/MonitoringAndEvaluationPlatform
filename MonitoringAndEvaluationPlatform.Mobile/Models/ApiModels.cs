namespace MonitoringAndEvaluationPlatform.Mobile.Models
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public UserProfile User { get; set; } = new();
    }

    public class RefreshTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? MinistryName { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }

    public class DashboardSummary
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

    public class FrameworkPerformance
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public double IndicatorsPerformance { get; set; }
        public int IndicatorCount { get; set; }
        public List<ProjectPerformance> Projects { get; set; } = new();
    }

    public class ProjectPerformance
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public double Performance { get; set; }
    }

    public class OutcomeProgress
    {
        public string OutcomeName { get; set; } = string.Empty;
        public int TotalIndicators { get; set; }
        public double TotalTarget { get; set; }
        public double TotalAchieved { get; set; }
        public double AchievementRate { get; set; }
    }

    public class ProjectProgress
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public double CompletionRate { get; set; }
        public int TotalIndicators { get; set; }
        public double TotalTarget { get; set; }
        public double TotalAchieved { get; set; }
    }

    public class PerformanceItem
    {
        public string Name { get; set; } = string.Empty;
        public int Code { get; set; }
        public double IndicatorsPerformance { get; set; }
        public double DisbursementPerformance { get; set; }
        public string? ParentName { get; set; }
    }

    public class TopBottomPerformers
    {
        public List<PerformanceItem> TopFrameworks { get; set; } = new();
        public List<PerformanceItem> TopOutcomes { get; set; } = new();
        public List<PerformanceItem> TopIndicators { get; set; } = new();
        public List<PerformanceItem> BottomFrameworks { get; set; } = new();
        public List<PerformanceItem> BottomOutcomes { get; set; } = new();
        public List<PerformanceItem> BottomIndicators { get; set; } = new();
    }

    public class CategoryReport
    {
        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public int ProjectCount { get; set; }
        public double TotalBudget { get; set; }
        public double AmountSpent { get; set; }
        public double IndicatorsPerformance { get; set; }
        public double DisbursementPerformance { get; set; }
        public double FieldMonitoring { get; set; }
        public double ImpactAssessment { get; set; }
    }

    public class BudgetOverview
    {
        public double TotalEstimatedBudget { get; set; }
        public double TotalRealBudget { get; set; }
    }

    public class PerformanceDistribution
    {
        public int HighPerformanceCount { get; set; }
        public int MediumPerformanceCount { get; set; }
        public int LowPerformanceCount { get; set; }
    }

    public class ReportsSummary
    {
        public int TotalFrameworks { get; set; }
        public int TotalOutcomes { get; set; }
        public int TotalOutputs { get; set; }
        public int TotalSubOutputs { get; set; }
        public int TotalIndicators { get; set; }
        public int TotalProjects { get; set; }
        public double AverageIndicatorsPerformance { get; set; }
        public double AverageDisbursementPerformance { get; set; }
        public List<PerformanceItem> FrameworkPerformanceData { get; set; } = new();
        public List<PerformanceItem> SectorPerformanceData { get; set; } = new();
        public List<PerformanceItem> MinistryPerformanceData { get; set; } = new();
        public BudgetOverview BudgetOverview { get; set; } = new();
        public PerformanceDistribution PerformanceDistribution { get; set; } = new();
    }

    public class LookupItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class LookupItemInt
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
