using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Measure
    {
        [Key]
        public int Code { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Value must be positive")]
        [Display(Name = "Value")]
        public double Value { get; set; }

        [Required]
        [Display(Name = "Value Type")]
        public MeasureValueType ValueType { get; set; } = MeasureValueType.Real;

        [Required]
        public int IndicatorCode { get; set; }
        virtual public Indicator Indicator { get; set; }

        // The value representing how much of the Indicator's target has been achieved
    }
}
