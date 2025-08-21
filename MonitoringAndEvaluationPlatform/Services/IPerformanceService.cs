namespace MonitoringAndEvaluationPlatform.Services
{
    public interface IPerformanceService
    {
        Task UpdateSubOutputPerformance(int subOutputCode);
        Task UpdateOutputPerformance(int outputCode);
        Task UpdateOutcomePerformance(int outcomeCode);
        Task UpdateFrameworkPerformance(int frameworkCode);
    }
}
