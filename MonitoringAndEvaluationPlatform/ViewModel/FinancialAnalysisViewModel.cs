namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class FinancialAnalysisViewModel
    {
        public List<ProjectFinancialItem> Projects { get; set; } = new();

        public double TotalEstimatedBudget { get; set; }
        public double TotalRealBudget { get; set; }
        public double AverageSpendingRate { get; set; }

        public int UnderSpendingCount { get; set; }
        public int OnTargetCount { get; set; }
        public int OverBudgetCount { get; set; }
        public int NotStartedCount { get; set; }
    }

    public class ProjectFinancialItem
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string MinistryName { get; set; } = string.Empty;
        public double EstimatedBudget { get; set; }
        public double RealBudget { get; set; }  // مجموع Plans.Realised
        public string Currency { get; set; } = "USD";
        public double SpendingRate => EstimatedBudget > 0
            ? Math.Round(RealBudget / EstimatedBudget * 100, 1)
            : 0;
        public double BudgetVariance => RealBudget - EstimatedBudget;

        public SpendingStatus Status => EstimatedBudget == 0 || RealBudget == 0
            ? SpendingStatus.NotStarted
            : SpendingRate > 100
                ? SpendingStatus.OverBudget
                : SpendingRate >= 80
                    ? SpendingStatus.OnTarget
                    : SpendingStatus.UnderSpending;
    }

    public enum SpendingStatus
    {
        NotStarted,
        UnderSpending,
        OnTarget,
        OverBudget
    }
}
