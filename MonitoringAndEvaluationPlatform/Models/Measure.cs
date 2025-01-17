using MonitoringAndEvaluationPlatform.Enums;
using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Measure
    {
        [Key]
        public int Code { get; set; }
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public MeasureValueType ValueType { get; set; }
        public int IndicatorCode { get; set; }
        virtual public Indicator Indicator { get; set; }
    }
}
