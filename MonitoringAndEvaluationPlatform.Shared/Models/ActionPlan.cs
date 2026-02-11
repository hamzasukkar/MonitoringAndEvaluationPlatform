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

        public void UpdatePerformance()
        {
            double totalPlanned = Activities.SelectMany(a => a.Plans).Sum(p => p.Planned);
            double totalRealised = Activities.SelectMany(a => a.Plans).Sum(p => p.Realised);

            if (Project != null) // Ensure Project is loaded
            {
                Project.UpdatePerformance(totalPlanned, totalRealised);
            }
        }

        public void DistributeBudgetEquallyToPlans()
        {
            if (Project?.EstimatedBudget > 0)
            {
                var allPlans = Activities.Where(a=>a.ActivityType==Enums.ActivityType.DisbursementPerformance).SelectMany(a => a.Plans).ToList();
                if (allPlans.Count > 0)
                {
                    int equalPlannedValue = (int)(Project.EstimatedBudget / allPlans.Count);
                    int remainder = (int)(Project.EstimatedBudget % allPlans.Count);

                    for (int i = 0; i < allPlans.Count; i++)
                    {
                        allPlans[i].Planned = equalPlannedValue;

                        // Add 1 to the first 'remainder' plans to distribute the remainder
                        if (i < remainder)
                        {
                            allPlans[i].Planned += 1;
                        }
                    }
                }
            }
        }



    }
}
