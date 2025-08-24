using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Sector
    {
        [Key]
        public int Code { get; set; }
        public string EN_Name { get; set; }
        public string AR_Name { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;

    }
}
