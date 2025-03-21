using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ActionPlan
    {
        [Key]
        public int Code { get; set; }
        public int PlansCount { get; set; }


        // Foreign key for one-to-one relationship
        public int ProjectID { get; set; }
        public Project Project { get; set; }

        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
