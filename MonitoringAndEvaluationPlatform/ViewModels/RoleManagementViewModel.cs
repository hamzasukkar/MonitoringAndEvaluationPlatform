namespace MonitoringAndEvaluationPlatform.ViewModels
{
    public class RoleManagementViewModel
    {
        public List<RoleViewModel> Roles { get; set; } = new();
        public int TotalRoles { get; set; }
        public int TotalUsers { get; set; }
    }
}
