using MonitoringAndEvaluationPlatform.Enums;
using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class LogicalMeasure
    {
        [Key]
        public int Code { get; set; }
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public MeasureValueType ValueType { get; set; }
        public int LogicalFrameworkIndicatorIndicatorCode { get; set; }
        virtual public LogicalFrameworkIndicator  LogicalFrameworkIndicator { get; set; }
        // Foreign keys

    }
}
