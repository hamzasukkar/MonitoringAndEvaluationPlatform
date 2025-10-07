using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class DashboardSummaryViewModel
    {
        public int TotalFrameworks { get; set; }
        public List<Framework> Frameworks { get; set; }
        public List<FrameworkPerformanceViewModel> FrameworksPerformance { get; set; } = new();
        public int TotlalMinistries { get; set; }
        public List<Ministry> Ministries { get; set; }
        public Dictionary<string, int> ProjectsByMinistry { get; set; } = new();
        public int TotalProjects { get; set; }
        public List<Project> Projects { get; set; }
        public int TotalGovernorate { get; set; }
        public List<Governorate> Governorates { get; set; }
        public Dictionary<string, int> ProjectsByGovernorate { get; set; } = new();
        public List<District> Districts { get; set; } = new();
        public List<SubDistrict> SubDistricts { get; set; } = new();
        public List<Community> Communities { get; set; } = new();
        public List<MonthlyPerformanceViewModel> MonthlyPerformance { get; set; } = new();
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    }

    public class MonthlyPerformanceViewModel
    {
        public string Month { get; set; }
        public double ProjectImplementation { get; set; }
        public double PerformanceIndicators { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string ActivityType { get; set; }
        public string ActivityTitle { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Icon { get; set; }
    }

}
