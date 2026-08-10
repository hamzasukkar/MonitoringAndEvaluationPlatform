using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProgramFilterViewModel
    {
        public List<int> SelectedMinistries { get; set; } = new List<int>();
        public List<int> SelectedRegions { get; set; } = new List<int>();
        public List<int> SelectedDonors { get; set; } = new List<int>();
        public List<int> SelectedGoals { get; set; } = new List<int>();
        public List<int> SelectedSectors { get; set; } = new List<int>();
        public List<int> SelectedPublicSectorTypes { get; set; } = new List<int>();
        public List<string> SelectedGovernorates { get; set; } = new List<string>();
        public List<Ministry> Ministries { get; set; } = new List<Ministry>();
        public List<Donor> Donors { get; set; } = new List<Donor>();
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<Sector> Sectors { get; set; } = new List<Sector>();
        public List<PublicSectorType> PublicSectorTypes { get; set; } = new List<PublicSectorType>();
        public List<Goal> Goals { get; set; } = new List<Goal>();
        public List<Governorate> Governorates { get; set; } = new List<Governorate>();

        public bool IsMinistryUser { get; set; } = false;

        // SubOutput filtering
        public int? SubOutputCode { get; set; }
        public string? SubOutputName { get; set; }

        // Hierarchy filter selections
        public int? SelectedFrameworkCode { get; set; }
        public int? SelectedOutcomeCode { get; set; }
        public int? SelectedOutputCode { get; set; }
        public int? SelectedSubOutputCode { get; set; }

        // Dropdown data for frameworks
        public List<Framework> Frameworks { get; set; } = new List<Framework>();

        // Display names for filter badges
        public string? FrameworkName { get; set; }
        public string? OutcomeName { get; set; }
        public string? OutputName { get; set; }

        // Search query
        public string? SearchQuery { get; set; }

        // Date filters
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }

        // Summary statistics
        public int TotalProjects { get; set; }
        public int NotStartedProjects { get; set; }
        public int InProgressProjects { get; set; }
        public int CompletedProjects { get; set; }
        public double TotalBudget { get; set; }

        // Sorting
        public string SortColumn { get; set; } = "performance";
        public string SortDirection { get; set; } = "desc";

        // Paging
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        public int FirstItemIndex => TotalRecords == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
        public int LastItemIndex => Math.Min(CurrentPage * PageSize, TotalRecords);

        public string GetSortDirection(string column) =>
            string.Equals(SortColumn, column, StringComparison.OrdinalIgnoreCase) && SortDirection == "asc" ? "desc" : "asc";

        public string GetSortIcon(string column) =>
            !string.Equals(SortColumn, column, StringComparison.OrdinalIgnoreCase) ? "fa-sort text-muted"
            : SortDirection == "asc" ? "fa-chevron-up" : "fa-chevron-down";
    }

}
