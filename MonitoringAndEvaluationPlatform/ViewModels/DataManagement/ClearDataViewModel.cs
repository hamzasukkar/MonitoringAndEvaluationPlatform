namespace MonitoringAndEvaluationPlatform.ViewModels.DataManagement
{
    public class ClearDataViewModel
    {
        public List<TableGroupViewModel> TableGroups { get; set; } = new();
        public List<string> SelectedGroups { get; set; } = new();
    }

    public class TableGroupViewModel
    {
        public string GroupName { get; set; } = string.Empty;
        public string GroupKey { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DangerLevel { get; set; } = string.Empty; // Low, Medium, High, Critical
        public List<string> Tables { get; set; } = new();
        public int TotalRecords { get; set; }
        public bool IsSelected { get; set; }
    }
}
