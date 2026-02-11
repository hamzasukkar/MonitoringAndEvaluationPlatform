using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class SDGIndicator
    {
        [Key]
        public int IndicatorCode { get; set; }
        public string EN_Name { get; set; }
        public string AR_Name { get; set; }
        public string Source { get; set; } = string.Empty;
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;

        public int TargetCode { get; set; }
        virtual public Target Target { get; set; }
        public bool IsCommon { get; set; }
        public bool Active { get; set; }

    }
}
