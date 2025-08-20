using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Goal
    {
        [Key]
        public int Code { get; set; }
        public string EN_Name { get; set; }
        public string AR_Name { get; set; }
        public string Icon { get; set; }
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;
        public ICollection<Target> Targets { get; set; } = new List<Target>();
    }
}
