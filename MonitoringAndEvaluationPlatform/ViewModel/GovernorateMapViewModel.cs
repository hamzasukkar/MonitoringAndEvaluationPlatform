namespace MonitoringAndEvaluationPlatform.ViewModel
{
    // ViewModel for the Governorate Map report (Reports/GovernorateMap).
    // Phase 1: clicking a governorate on the Syria map shows its projects in a table.
    public class GovernorateMapViewModel
    {
        public List<GovernorateProjectsItem> Governorates { get; set; } = new();
        public List<GeoProjectItem> NationalProjects { get; set; } = new();
        public int TotalProjects { get; set; }
    }

    public class GovernorateProjectsItem
    {
        public string Code { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public List<GeoProjectItem> Projects { get; set; } = new();
    }

    public class GeoProjectItem
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Ministry { get; set; } = string.Empty;
        public double EstimatedBudget { get; set; }
        public string Currency { get; set; } = "USD";
        public double Performance { get; set; }
        public double DisbursementPerformance { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public bool IsNational { get; set; }
    }
}
