namespace MonitoringAndEvaluationPlatform.ViewModel
{
    // ViewModel for the Governorate Map report (Reports/GovernorateMap).
    // A flat projects list drives the table, map colouring, highlighting and the cascading
    // Strategy / Ministry / Project filters (all filtering is done client-side in JS).
    public class GovernorateMapViewModel
    {
        public List<GovernorateRef> Governorates { get; set; } = new();
        public List<GeoProjectItem> Projects { get; set; } = new();
        public List<StrategyRef> Strategies { get; set; } = new();
        public List<MinistryRef> Ministries { get; set; } = new();
        public int TotalProjects { get; set; }
    }

    public class GovernorateRef
    {
        public string Code { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
    }

    public class StrategyRef
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class MinistryRef
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class GeoProjectItem
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int? MinistryCode { get; set; }
        public string Ministry { get; set; } = string.Empty;
        public double EstimatedBudget { get; set; }
        public string Currency { get; set; } = "USD";
        public double Performance { get; set; }
        public double DisbursementPerformance { get; set; }
        public double TotalRealised { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public bool IsNational { get; set; }

        // Relationships used for client-side filtering / highlighting
        public List<int> FrameworkCodes { get; set; } = new();
        public List<string> GovernorateCodes { get; set; } = new();
    }
}
