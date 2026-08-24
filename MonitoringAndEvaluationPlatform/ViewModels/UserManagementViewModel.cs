namespace MonitoringAndEvaluationPlatform.ViewModels
{
    public class UserManagementViewModel
    {
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public string SearchTerm { get; set; } = string.Empty;
        public string RoleFilter { get; set; } = string.Empty;
        public string MinistryFilter { get; set; } = string.Empty;
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPages { get; set; }
        public List<string> AvailableRoles { get; set; } = new List<string>();
        public List<MonitoringAndEvaluationPlatform.Models.Ministry> AvailableMinistries { get; set; } = new List<MonitoringAndEvaluationPlatform.Models.Ministry>();
    }
}
