using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Donor
    {
        [Key]
        public int Code { get; set; }
        public string Partner { get; set; }
        public DonorCategory donorCategory { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<ProjectDonor> ProjectDonors { get; set; } = new List<ProjectDonor>();
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;
    }
}
