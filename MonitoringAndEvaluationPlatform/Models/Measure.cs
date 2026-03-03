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

        public int ProjectPhaseId { get; set; }
        public virtual ProjectPhase ProjectPhase { get; set; } = null!;
    }
}
