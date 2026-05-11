using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ActionPlan
    {
        [Key]
        public int Code { get; set; }
        public int PlansCount { get; set; }

        // Foreign key for one-to-one relationship with ProjectPhase
        public int ProjectPhaseId { get; set; }
        public virtual ProjectPhase ProjectPhase { get; set; } = null!;

        public ICollection<Plan> Plans { get; set; } = new List<Plan>();
    }
}
