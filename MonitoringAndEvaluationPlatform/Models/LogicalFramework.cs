using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class LogicalFramework
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public LogicalFrameworkType Type { get; set; }
        public double Trend { get; set; } = 0;
        public int IndicatorsPerformance { get; set; } = 0;
        public ICollection<LogicalFrameworkIndicator> logicalFrameworkIndicators  { get; set; } = new List<LogicalFrameworkIndicator>();
        public int ProjectID { get; set; } 

        //virtual public Program Program { get; set; }
    }
}
