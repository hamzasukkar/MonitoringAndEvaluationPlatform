using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ActionPlan
    {
        [Key]
        public int Code { get; set; }
        public int PlansCount { get; set; }
        public ICollection<Plan> Plans { get; set; } = new List<Plan>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
