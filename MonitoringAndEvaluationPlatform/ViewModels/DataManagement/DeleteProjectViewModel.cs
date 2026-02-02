namespace MonitoringAndEvaluationPlatform.ViewModels.DataManagement
{
    public class DeleteProjectViewModel
    {
        public List<ProjectForDeletionViewModel> Projects { get; set; } = new();
        public List<int> SelectedProjectIds { get; set; } = new();
        public string? SearchTerm { get; set; }
    }

    public class ProjectForDeletionViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int ActionPlanCount { get; set; }
        public int ActivityCount { get; set; }
        public int PlanCount { get; set; }
        public int IndicatorCount { get; set; }
        public int FileCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? MinistryName { get; set; }
    }
}
