namespace MonitoringAndEvaluationPlatform.ViewModels
{
    public class RoleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public List<string> Permissions { get; set; } = new();
        public List<UserViewModel> Users { get; set; } = new();
    }
}
