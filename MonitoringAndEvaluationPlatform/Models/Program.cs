using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Program
    {
        [Key]
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public double EstimatedBudget { get; set; }
        public double RealBudget { get; set; }
        public string Trend { get; set; }
        public int performance { get; set; }
        public int DisbursementPerformance { get; set; }
        public int FieldMonitoring { get; set; }
        public int ImpactAssessment { get; set; }
    }
}
