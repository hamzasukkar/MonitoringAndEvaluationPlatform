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

        // The ministry that owns this strategy. Drives the ministry scoping applied
        // across the dashboard, monitoring, reports and tree controllers.
        public int? MinistryCode { get; set; }

        [ForeignKey(nameof(MinistryCode))]
        public Ministry? Ministry { get; set; }

        // Navigation property for related Outcomes
        public ICollection<Outcome> Outcomes { get; set; } = new List<Outcome>();

        // Navigation property for related Goals
        public ICollection<FrameworkGoal> Goals { get; set; } = new List<FrameworkGoal>();

    }
}
