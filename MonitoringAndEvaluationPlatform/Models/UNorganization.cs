using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class UNorganization
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }

        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;
    }
}
