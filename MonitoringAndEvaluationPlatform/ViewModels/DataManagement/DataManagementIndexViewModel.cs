namespace MonitoringAndEvaluationPlatform.ViewModels.DataManagement
{
    public class DataManagementIndexViewModel
    {
        // Database Statistics
        public int TotalProjects { get; set; }
        public int TotalFrameworks { get; set; }
        public int TotalIndicators { get; set; }
        public int TotalPlans { get; set; }
        public int TotalMeasures { get; set; }
        public int TotalAuditLogs { get; set; }

        // Size Estimates
        public long DatabaseSizeKB { get; set; }

        // Last Operations
        public DateTime? LastBackupDate { get; set; }
    }
}
