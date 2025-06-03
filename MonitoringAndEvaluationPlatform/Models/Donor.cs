using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Donor
    {
        [Key]
        public int Code { get; set; }
        public string Partner { get; set; }

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public int DisbursementPerformance { get; set; } = 0;
        public int FieldMonitoring { get; set; } = 0;
        public int ImpactAssessment { get; set; } = 0;
    }
}
