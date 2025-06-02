using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;

        public ActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateActivityAsync(Activity activity)
        {
            var actionPlan = await _context.ActionPlans
                .FirstOrDefaultAsync(ap => ap.Code == activity.ActionPlanCode);

            if (actionPlan == null)
            {
                return false; // ActionPlan not found
            }

            // Generate Plans based on PlansCount
            for (int i = 1; i <= actionPlan.PlansCount; i++)
            {
                var plan = new Plan
                {
                    Name = $"{activity.ActivityType}-{i}", // Example: DisbursementPerformance-1
                    Date = DateTime.Now,
                    Planned = 0,
                    Realised = 0,
                    Activity = activity
                };
                activity.Plans.Add(plan);
            }

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CreateActivitiesForAllTypesAsync(Activity baseActivity)
        {
            var actionPlan = await _context.ActionPlans
                .FirstOrDefaultAsync(ap => ap.Code == baseActivity.ActionPlanCode);

            if (actionPlan == null)
            {
                return false; // ActionPlan not found
            }

            foreach (ActivityType type in Enum.GetValues(typeof(ActivityType)))
            {
                var activity = new Activity
                {
                    Name = $"{baseActivity.Name}-{type}", // Example: "Road Construction-DisbursementPerformance"
                    ActionPlanCode = baseActivity.ActionPlanCode,
                    ActivityType = type
                };

                for (int i = 1; i <= actionPlan.PlansCount; i++)
                {
                    var plan = new Plan
                    {
                        Name = $"{type}-{i}",
                        Date = DateTime.Now,
                        Planned = 0,
                        Realised = 0,
                        Activity = activity
                    };
                    activity.Plans.Add(plan);
                }

                _context.Activities.Add(activity);
            }

            await _context.SaveChangesAsync();
            return true;
        }

    }

}
