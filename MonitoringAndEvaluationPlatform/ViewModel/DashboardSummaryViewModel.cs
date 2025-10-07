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
        public Dictionary<string, int> ProjectsByDonor { get; set; } = new();
        public List<DonorPerformanceViewModel> DonorsPerformance { get; set; } = new();
        public Dictionary<string, int> ProjectsBySector { get; set; } = new();
        public List<SectorPerformanceViewModel> SectorsPerformance { get; set; } = new();
        public List<ProjectPerformanceViewModel> ProjectsPerformance { get; set; } = new();
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

    public class DonorPerformanceViewModel
    {
        public int Code { get; set; }
        public string Partner { get; set; }
        public double OverallPerformance { get; set; }
        public double IndicatorsPerformance { get; set; }
        public double DisbursementPerformance { get; set; }
        public double FieldMonitoring { get; set; }
        public double ImpactAssessment { get; set; }
    }

    public class SectorPerformanceViewModel
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public double OverallPerformance { get; set; }
        public double IndicatorsPerformance { get; set; }
        public double DisbursementPerformance { get; set; }
        public double FieldMonitoring { get; set; }
        public double ImpactAssessment { get; set; }
    }

    public class ProjectPerformanceViewModel
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public double Performance { get; set; }
        public double DisbursementPerformance { get; set; }
        public double FieldMonitoring { get; set; }
        public double ImpactAssessment { get; set; }
    }

}
