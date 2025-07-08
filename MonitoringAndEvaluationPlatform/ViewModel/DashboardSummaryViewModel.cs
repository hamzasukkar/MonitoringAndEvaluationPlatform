using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class DashboardSummaryViewModel
    {
        public int TotalFrameworks { get; set; }
        public List<Framework> Frameworks { get; set; }
        public int TotlalMinistries { get; set; }
        public List<Ministry> Ministries { get; set; }
        public int TotalProjects { get; set; }
        public List<Project> Projects { get; set; }
        public int TotalGovernorate { get; set; }
        public List<Governorate> Governorates { get; set; }
        public List<District> Districts { get; set; } = new();
        public List<SubDistrict> SubDistricts { get; set; } = new();
        public List<Community> Communities { get; set; } = new();
    }

}
