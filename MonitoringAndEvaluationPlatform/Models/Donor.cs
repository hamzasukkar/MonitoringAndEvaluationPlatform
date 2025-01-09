using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Donor
    {
        [Key]
        public int Code { get; set; }
        public string Partner { get; set; }
        public int DisbursementPerformance { get; set; }
        public int FieldMonitoring { get; set; }
        public int ImpactAssessment { get; set; }
    }
}
