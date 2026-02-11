using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ProjectIndicator
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public int IndicatorCode { get; set; }
        public Indicator Indicator { get; set; }
    }
}
