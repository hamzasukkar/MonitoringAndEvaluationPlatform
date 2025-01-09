using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Indicator
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public double Trend { get; set; } = 0;
        public int IndicatorsPerformance { get; set; } = 0;

        [Range(0, 1, ErrorMessage = "The Weight must be between 0 and 1.")]
        public double Weight { get; set; } = 1;
        public int SubOutputCode { get; set; }
        virtual public SubOutput SubOutput { get; set; }

    }
}
