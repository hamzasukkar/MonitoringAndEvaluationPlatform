using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Ministry

    {
        [Key]
        public int Code { get; set; }
        public string MinistryName { get; set; }
        public int DisbursementPerformance { get; set; } = 0;
        public int FieldMonitoring { get; set; } = 0;
        public int ImpactAssessment { get; set; } = 0;
    }
}
