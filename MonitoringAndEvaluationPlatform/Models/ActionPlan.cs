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

        public ICollection<Activity> Activities { get; set; } = new List<Activity>();

        public void UpdatePerformance()
        {
            double totalPlanned = Activities.SelectMany(a => a.Plans).Sum(p => p.Planned);
            double totalRealised = Activities.SelectMany(a => a.Plans).Sum(p => p.Realised);

            if (ProjectPhase?.Project != null)
            {
                // Performance cascade is handled by PlanService/MonitoringService
                // This method kept for compatibility with Plan.UpdatePerformance() call chain
            }
        }

        public void DistributeBudgetEquallyToPlans()
        {
            if (ProjectPhase?.Project?.EstimatedBudget > 0)
            {
                var allPlans = Activities.SelectMany(a => a.Plans).ToList();
                if (allPlans.Count > 0)
                {
                    int equalPlannedValue = (int)(ProjectPhase.Project.EstimatedBudget / allPlans.Count);
                    int remainder = (int)(ProjectPhase.Project.EstimatedBudget % allPlans.Count);

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
