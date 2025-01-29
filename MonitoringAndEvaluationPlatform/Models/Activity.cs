using Microsoft.Build.Evaluation;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Activity
    {
        public string Name { get; set; }

        public ICollection<Plan> Plans { get; set; } = new List<Plan>();

        public ActivityType ActivityType { get; set; }
    }
}
