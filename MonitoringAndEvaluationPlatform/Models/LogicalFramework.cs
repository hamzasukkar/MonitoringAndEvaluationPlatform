using MonitoringAndEvaluationPlatform.Enums;
using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class LogicalFramework
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public LogicalFrameworkType Type { get; set; }
        public double Performance { get; set; } = 0;
        public ICollection<LogicalFrameworkIndicator> logicalFrameworkIndicators { get; set; } = new List<LogicalFrameworkIndicator>();
        public int ProjectID { get; set; }
    }
}
