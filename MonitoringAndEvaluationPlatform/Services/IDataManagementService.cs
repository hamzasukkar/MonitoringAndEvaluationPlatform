using MonitoringAndEvaluationPlatform.ViewModels.DataManagement;

namespace MonitoringAndEvaluationPlatform.Services
{
    public interface IDataManagementService
    {
        Task<DataManagementIndexViewModel> GetIndexStatisticsAsync();
        Task<string> GenerateDatabaseBackupScriptAsync(bool includeIdentityData, bool includeAuditLogs);
        Task<ClearDataViewModel> GetTableGroupsAsync();
        Task<int> ClearTablesAsync(List<string> groupKeys);
        Task<DeleteProjectViewModel> GetProjectsForDeletionAsync(string? searchTerm = null);
        Task<int> DeleteProjectsAsync(List<int> projectIds);
        Task<ResetValuesViewModel> GetResetValuesStatisticsAsync();
        Task<int> ResetValuesAsync(bool resetPlans, bool resetMeasures, bool resetProjectPerformance, bool recalculateHierarchy);
    }
}
