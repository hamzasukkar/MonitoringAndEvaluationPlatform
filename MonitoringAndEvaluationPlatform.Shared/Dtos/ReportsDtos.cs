namespace MonitoringAndEvaluationPlatform.Dtos
{
    public class ReportsSummaryDto
    {
        public int TotalFrameworks { get; set; }
        public int TotalOutcomes { get; set; }
        public int TotalOutputs { get; set; }
        public int TotalSubOutputs { get; set; }
        public int TotalIndicators { get; set; }
        public int TotalProjects { get; set; }
        public double AverageIndicatorsPerformance { get; set; }
        public double AverageDisbursementPerformance { get; set; }
        public List<PerformanceItemDto> FrameworkPerformanceData { get; set; } = new();
        public List<PerformanceItemDto> SectorPerformanceData { get; set; } = new();
        public List<PerformanceItemDto> MinistryPerformanceData { get; set; } = new();
        public BudgetOverviewDto BudgetOverview { get; set; } = new();
        public PerformanceDistributionDto PerformanceDistribution { get; set; } = new();
    }

    public class PerformanceItemDto
    {
        public string Name { get; set; } = string.Empty;
        public int Code { get; set; }
        public double IndicatorsPerformance { get; set; }
        public double DisbursementPerformance { get; set; }
        public string? ParentName { get; set; }
    }

    public class TopBottomPerformersDto
    {
        public List<PerformanceItemDto> TopFrameworks { get; set; } = new();
        public List<PerformanceItemDto> TopOutcomes { get; set; } = new();
        public List<PerformanceItemDto> TopOutputs { get; set; } = new();
        public List<PerformanceItemDto> TopSubOutputs { get; set; } = new();
        public List<PerformanceItemDto> TopIndicators { get; set; } = new();
        public List<PerformanceItemDto> BottomFrameworks { get; set; } = new();
        public List<PerformanceItemDto> BottomOutcomes { get; set; } = new();
        public List<PerformanceItemDto> BottomOutputs { get; set; } = new();
        public List<PerformanceItemDto> BottomSubOutputs { get; set; } = new();
        public List<PerformanceItemDto> BottomIndicators { get; set; } = new();
    }

    public class CategoryReportDto
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

    public class BudgetOverviewDto
    {
        public double TotalEstimatedBudget { get; set; }
        public double TotalRealBudget { get; set; }
    }

    public class PerformanceDistributionDto
    {
        public int HighPerformanceCount { get; set; }
        public int MediumPerformanceCount { get; set; }
        public int LowPerformanceCount { get; set; }
    }
}
