namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ReportsDashboardViewModel
    {
        // Summary Counts
        public int TotalFrameworks { get; set; }
        public int TotalOutcomes { get; set; }
        public int TotalOutputs { get; set; }
        public int TotalSubOutputs { get; set; }
        public int TotalIndicators { get; set; }
        public int TotalProjects { get; set; }

        // Average Performance
        public double AverageIndicatorsPerformance { get; set; }
        public double AverageDisbursementPerformance { get; set; }

        // Framework Performance Data (for charts)
        public List<PerformanceDataItem> FrameworkPerformanceData { get; set; } = new List<PerformanceDataItem>();

        // Top 5 Performers
        public List<PerformanceDataItem> TopFrameworks { get; set; } = new List<PerformanceDataItem>();
        public List<PerformanceDataItem> TopOutcomes { get; set; } = new List<PerformanceDataItem>();
        public List<PerformanceDataItem> TopOutputs { get; set; } = new List<PerformanceDataItem>();
        public List<PerformanceDataItem> TopSubOutputs { get; set; } = new List<PerformanceDataItem>();
        public List<PerformanceDataItem> TopIndicators { get; set; } = new List<PerformanceDataItem>();

        // Bottom 5 Performers
        public List<PerformanceDataItem> BottomFrameworks { get; set; } = new List<PerformanceDataItem>();
        public List<PerformanceDataItem> BottomOutcomes { get; set; } = new List<PerformanceDataItem>();
        public List<PerformanceDataItem> BottomOutputs { get; set; } = new List<PerformanceDataItem>();
        public List<PerformanceDataItem> BottomSubOutputs { get; set; } = new List<PerformanceDataItem>();
        public List<PerformanceDataItem> BottomIndicators { get; set; } = new List<PerformanceDataItem>();

        // Performance Distribution
        public int HighPerformanceCount { get; set; }
        public int MediumPerformanceCount { get; set; }
        public int LowPerformanceCount { get; set; }

        // New Chart Data
        public List<PerformanceDataItem> SectorPerformanceData { get; set; } = new List<PerformanceDataItem>();
        public List<PerformanceDataItem> MinistryPerformanceData { get; set; } = new List<PerformanceDataItem>();
        public List<ProjectScatterDataItem> ProjectScatterData { get; set; } = new List<ProjectScatterDataItem>();
        public BudgetOverviewItem BudgetOverview { get; set; } = new BudgetOverviewItem();

        // Reports by Category
        public List<CategoryReportItem> MinistryReports { get; set; } = new List<CategoryReportItem>();
        public List<CategoryReportItem> SectorReports { get; set; } = new List<CategoryReportItem>();
        public List<CategoryReportItem> PublicSectorTypeReports { get; set; } = new List<CategoryReportItem>();
        public List<CategoryReportItem> DonorReports { get; set; } = new List<CategoryReportItem>();
        public List<CategoryReportItem> SupervisorReports { get; set; } = new List<CategoryReportItem>();
        public List<CategoryReportItem> ProjectManagerReports { get; set; } = new List<CategoryReportItem>();
        public List<CategoryReportItem> GovernorateReports { get; set; } = new List<CategoryReportItem>();

        // Totals for category summaries
        public int TotalMinistries { get; set; }
        public int TotalSectors { get; set; }
        public int TotalPublicSectorTypes { get; set; }
        public int TotalDonors { get; set; }
        public int TotalSupervisors { get; set; }
        public int TotalProjectManagers { get; set; }
        public int TotalGovernorates { get; set; }
    }

    public class ProjectScatterDataItem
    {
        public string ProjectName { get; set; }
        public double FinancialProgress { get; set; }
        public double PhysicalProgress { get; set; }
        public double Budget { get; set; }
    }

    public class BudgetOverviewItem
    {
        public double TotalEstimatedBudget { get; set; }
        public double TotalRealBudget { get; set; }
    }

    public class PerformanceDataItem
    {
        public string Name { get; set; } = string.Empty;
        public int Code { get; set; }
        public double IndicatorsPerformance { get; set; }
        public double DisbursementPerformance { get; set; }
        public string? ParentName { get; set; }
    }

    public class CategoryReportItem
    {
        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public int ProjectCount { get; set; }
        public double TotalBudget { get; set; }
        public double AmountSpent { get; set; }
        public double IndicatorsPerformance { get; set; }
        public double DisbursementPerformance { get; set; }
    }
}
