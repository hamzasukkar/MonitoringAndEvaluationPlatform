using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.ViewModels.DataManagement
{
    public class BackupViewModel
    {
        public bool IncludeIdentityData { get; set; } = false;
        public bool IncludeAuditLogs { get; set; } = true;

        // Statistics for display
        public int TotalTables { get; set; }
        public int TotalRecords { get; set; }
    }
}
