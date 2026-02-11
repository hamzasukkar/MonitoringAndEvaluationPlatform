namespace MonitoringAndEvaluationPlatform.Services
{
    public interface IPerformanceService
    {
        // IndicatorsPerformance methods
        Task UpdateSubOutputPerformance(int subOutputCode);
        Task UpdateOutputPerformance(int outputCode);
        Task UpdateOutcomePerformance(int outcomeCode);
        Task UpdateFrameworkPerformance(int frameworkCode);

        // DisbursementPerformance methods (also handles FieldMonitoring and ImpactAssessment)
        Task UpdateSubOutputDisbursementPerformance(int subOutputCode);
        Task UpdateOutputDisbursementPerformance(int outputCode);
        Task UpdateOutcomeDisbursementPerformance(int outcomeCode);
        Task UpdateFrameworkDisbursementPerformance(int frameworkCode);
    }
}
