using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Outcome
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public double Trend { get; set; }
        public int IndicatorsPerformance { get; set; }
        public int DisbursementPerformance { get; set; }
        public int FieldMonitoring { get; set; }
        public int ImpactAssessment { get; set; }

        // Foreign key and navigation property for Framework
        public int FrameworkCode { get; set; }
        virtual public Framework Framework { get; set; }
        public ICollection<Output> Outputs { get; set; } = new List<Output>();
    }
}
