using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class IndicatorListViewModel
    {
        public List<Indicator> Indicators { get; set; } = new();

        // Filters
        public int? FrameworkCode { get; set; }
        public int? OutcomeCode { get; set; }
        public int? OutputCode { get; set; }
        public int? SubOutputCode { get; set; }
        public string? SearchString { get; set; }
        public string? PerformanceBand { get; set; }
        public string? DisbursementBand { get; set; }

        // Filter dropdown options
        public List<Outcome> Outcomes { get; set; } = new();
        public List<Output> Outputs { get; set; } = new();
        public List<SubOutput> SubOutputs { get; set; } = new();

        // Sorting
        public string SortColumn { get; set; } = "Name";
        public string SortDirection { get; set; } = "asc";

        // Paging
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        public int FirstItemIndex => TotalRecords == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
        public int LastItemIndex => Math.Min(CurrentPage * PageSize, TotalRecords);

        // Helper method to get sort direction for a column
        public string GetSortDirection(string column)
        {
            if (string.Equals(SortColumn, column, StringComparison.OrdinalIgnoreCase))
            {
                return SortDirection == "asc" ? "desc" : "asc";
            }
            return "asc";
        }

        // Helper method to get sort icon for a column
        public string GetSortIcon(string column)
        {
            if (!string.Equals(SortColumn, column, StringComparison.OrdinalIgnoreCase))
                return "fa-sort text-muted";
            return SortDirection == "asc" ? "fa-chevron-up" : "fa-chevron-down";
        }
    }
}
