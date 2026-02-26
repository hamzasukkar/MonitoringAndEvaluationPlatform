using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Measure
    {
        [Key]
        public int Code { get; set; }
        public DateTime Date { get; set; }
        [Range(0, 100, ErrorMessage = "Value must be between 0 and 100.")]
        public double Value { get; set; }
        public int IndicatorCode { get; set; }
        virtual public Indicator Indicator { get; set; }

        // The value representing how much of the Indicator's target has been achieved
    }
}
