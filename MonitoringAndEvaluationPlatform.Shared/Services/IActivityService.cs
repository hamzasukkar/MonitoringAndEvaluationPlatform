using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public interface IActivityService
    {
        Task<bool> CreateActivitiesForAllTypesAsync(Activity baseActivity);
    }
}
