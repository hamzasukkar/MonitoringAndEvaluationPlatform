using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModels
{
    public class AuditLogViewModel
    {
        public List<AuditLog> AuditLogs { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? EntityFilter { get; set; }
        public string? ActionFilter { get; set; }
        public string? UserFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public List<string> AvailableEntities { get; set; } = new();
        public List<string> AvailableActions { get; set; } = new() { "Create", "Update", "Delete" };
        public List<string> AvailableUsers { get; set; } = new();

        // Sorting
        public string? SortColumn { get; set; } = "Timestamp";
        public string? SortDirection { get; set; } = "desc";

        // When false (default) Authentication logs are hidden unless an entity filter is set
        public bool IncludeAuthentication { get; set; }

        // Statistics
        public int TotalAuditLogs { get; set; }
        public int TodayLogs { get; set; }
        public int CreateCount { get; set; }
        public int UpdateCount { get; set; }
        public int DeleteCount { get; set; }

        // Helper method to get sort direction for a column
        public string GetSortDirection(string column)
        {
            if (SortColumn == column)
            {
                return SortDirection == "asc" ? "desc" : "asc";
            }
            return "asc";
        }

        // Helper method to get sort icon for a column
        public string GetSortIcon(string column)
        {
            if (SortColumn != column)
                return "fa-sort";
            return SortDirection == "asc" ? "fa-sort-up" : "fa-sort-down";
        }
    }
}
