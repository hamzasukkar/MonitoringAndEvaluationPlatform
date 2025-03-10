using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Indicator
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public string Source { get; set; } = string.Empty;
        public double Trend { get; set; } = 0;
        public int IndicatorsPerformance { get; set; } = 0;

        [Range(0, 1, ErrorMessage = "The Weight must be between 0 and 1.")]
        public double Weight { get; set; } = 1;
        public int SubOutputCode { get; set; }
        virtual public SubOutput SubOutput { get; set; }
        public bool IsCommon { get; set; }
        public bool Active { get; set; }
        public int Target { get; set; } = 0;

        [DataType(DataType.Date)]
        public DateTime TargetYear { get; set; }
        public double GAGRA { get; set; } = 0;
        public double GAGRR { get; set; } = 0;
        public string Concept { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MethodOfComputation { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public ICollection<Measure> Measures { get; set; } = new List<Measure>();

        //virtual public Program Program { get; set; }

    }
}
