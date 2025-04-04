using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Plan
    {
        [Key]
        public int Code { get; set; }

        public string Name { get; set; }

        public DateTime Date { get; set; }

        public int Planned { get; set; }

        public int Realised { get; set; }

        public int ActivityCode { get; set; }

        virtual public Activity Activity { get; set; }

        public void UpdatePerformance()
        {
            if (Activity?.ActionPlan != null) // Ensure ActionPlan exists
            {
                Activity.ActionPlan.UpdatePerformance();
            }
        }
    }
}
