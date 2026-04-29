using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Framework
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;

        [Range(0, 1, ErrorMessage = "The Weight must be between 0 and 1.")]

        public int? MinistryCode { get; set; }

        [ForeignKey(nameof(MinistryCode))]
        public Ministry? Ministry { get; set; }

        // Navigation property for related Outcomes
        public ICollection<Outcome> Outcomes { get; set; } = new List<Outcome>();

        // Navigation property for related Goals
        public ICollection<FrameworkGoal> Goals { get; set; } = new List<FrameworkGoal>();

    }
}
