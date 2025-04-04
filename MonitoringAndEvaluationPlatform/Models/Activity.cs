using Microsoft.Build.Evaluation;
using MonitoringAndEvaluationPlatform.Enums;
using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Activity
    {
        [Key]
        public int Code { get; set; }

        public string Name { get; set; }

        public ICollection<Plan> Plans { get; set; } = new List<Plan>();

        public ActivityType ActivityType { get; set; }

        public int ActionPlanCode { get; set; }

        virtual public ActionPlan ActionPlan { get; set; }
    }
}
