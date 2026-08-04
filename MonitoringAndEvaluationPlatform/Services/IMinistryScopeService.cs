using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// Single source of truth for "which ministry's data may the current user touch?".
    ///
    /// Four controllers previously each carried their own private copy of this logic, and
    /// several controllers that load records by ID had no copy at all - which is how an
    /// authenticated user of one ministry could read and overwrite another ministry's
    /// measures, plans, phases and action plans by guessing an ID.
    ///
    /// Every method here FAILS CLOSED: a non-administrator with no MinistryCode is scoped to
    /// nothing rather than to everything.
    /// </summary>
    public interface IMinistryScopeService
    {
        /// <summary>
        /// Resolves the current user's scope. IsAdmin means unrestricted; otherwise
        /// MinistryCode is the only ministry whose data may be accessed (null = no access).
        /// </summary>
        Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync();

        /// <summary>Restricts a framework query to the caller's ministry.</summary>
        Task<IQueryable<Framework>> ApplyFrameworkScopeAsync(IQueryable<Framework> query);

        /// <summary>Restricts a project query to the caller's ministry.</summary>
        Task<IQueryable<Project>> ApplyProjectScopeAsync(IQueryable<Project> query);

        Task<bool> ProjectBelongsToScopeAsync(int projectId);

        Task<bool> SubOutputBelongsToScopeAsync(int subOutputCode);

        /// <summary>ProjectPhase.ProjectID -> Project.MinistryCode</summary>
        Task<bool> ProjectPhaseBelongsToScopeAsync(int projectPhaseId);

        /// <summary>Measure.ProjectPhaseId -> ProjectPhase -> Project.MinistryCode</summary>
        Task<bool> MeasureBelongsToScopeAsync(int measureCode);

        /// <summary>ActionPlan.ProjectPhaseId -> ProjectPhase -> Project.MinistryCode</summary>
        Task<bool> ActionPlanBelongsToScopeAsync(int actionPlanCode);

        /// <summary>Plan.ActionPlanCode -> ActionPlan -> ProjectPhase -> Project.MinistryCode</summary>
        Task<bool> PlanBelongsToScopeAsync(int planCode);
    }
}
