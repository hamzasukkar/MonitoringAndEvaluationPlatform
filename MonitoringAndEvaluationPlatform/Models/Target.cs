using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Target
    {
        [Key]
        public int Code { get; set; }
        public string EN_Name { get; set; }
        public string AR_Name { get; set; }
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;

        public int GoalCode { get; set; }
        virtual public Goal Goal { get; set; }
        public ICollection<SDGIndicator> SDGsIndicators { get; set; } = new List<SDGIndicator>();
    }
}
