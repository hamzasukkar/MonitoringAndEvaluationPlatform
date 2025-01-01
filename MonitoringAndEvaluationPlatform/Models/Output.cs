using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Output
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public double Trend { get; set; }
        public int IndicatorsPerformance { get; set; }
        public int DisbursementPerformance { get; set; }
        public int FieldMonitoring { get; set; }
        public int ImpactAssessment { get; set; }
        public int OutcomeCode { get; set; }
        virtual public Outcome Outcome { get; set; }
        public ICollection<SubOutput> SubOutputs{ get; set; } = new List<SubOutput>();
    }
}
