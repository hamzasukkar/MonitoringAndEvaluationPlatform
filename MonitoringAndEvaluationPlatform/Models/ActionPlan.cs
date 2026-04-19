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

        public void UpdatePerformance()
        {
            double totalPlanned = Plans.Sum(p => p.Planned);
            double totalRealised = Plans.Sum(p => p.Realised);

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
                var allPlans = Plans.ToList();
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
