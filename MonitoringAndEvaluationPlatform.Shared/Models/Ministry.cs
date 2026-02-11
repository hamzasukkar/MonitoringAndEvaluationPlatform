using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Ministry

    {
        [Key]
        public int Code { get; set; }
        public string MinistryUserName { get; set; }
        public string MinistryDisplayName_AR { get; set; }
        public string MinistryDisplayName_EN { get; set; }
        public string Logo { get; set; }
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
