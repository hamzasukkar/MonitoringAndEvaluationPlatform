using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Measure
    {
        [Key]
        public int Code { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        [Range(0, 100, ErrorMessage = "Value must be between 0 and 100.")]
        public double Value { get; set; }

        [StringLength(1000)]
        public string? Note { get; set; }

        public double? Quantity { get; set; }

        [StringLength(100)]
        public string? Unit { get; set; }

        public int ProjectPhaseId { get; set; }
        public virtual ProjectPhase ProjectPhase { get; set; } = null!;
    }
}
