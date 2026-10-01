namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// What the current user may see. A SystemAdministrator sees everything; anyone else is
    /// confined to their own ministry, whatever their role. A non-admin with no MinistryCode sees
    /// nothing, and a row with no owner is visible to administrators only — both sides of a
    /// comparison must be a real, equal ministry code for access to be granted.
    /// </summary>
    public sealed record MinistryScope(bool IsAdmin, int? MinistryCode)
    {
        public static readonly MinistryScope Unrestricted = new(true, null);
        public static readonly MinistryScope Nothing = new(false, null);

        public bool IsRestricted => !IsAdmin;

        /// <summary>A non-admin who is not assigned to any ministry: fails closed.</summary>
        public bool SeesNothing => !IsAdmin && MinistryCode is null;

        public bool CanSee(int? ownerMinistryCode) =>
            IsAdmin || (MinistryCode is int own && ownerMinistryCode == own);

        /// <summary>
        /// Guard for code that already holds the deconstructed (isAdmin, scopedMinistryCode) pair.
        /// Replaces <c>!isAdmin &amp;&amp; owner != scoped</c>, which let a user with no ministry
        /// through to every row with no owner, because <c>null != null</c> is false.
        /// </summary>
        public static bool Allows(bool isAdmin, int? scopedMinistryCode, int? ownerMinistryCode) =>
            new MinistryScope(isAdmin, scopedMinistryCode).CanSee(ownerMinistryCode);
    }

    /// <summary>
    /// The single source of the current user's ministry scope. Reads are filtered with the
    /// <c>WithinScope</c> extensions in <see cref="MinistryScopeQueryExtensions"/>; actions that
    /// load a record by id guard it with the CanAccess* methods.
    ///
    /// Deliberately not an EF global query filter: the performance cascades in MonitoringService
    /// read every ministry's projects and write shared Sector/Donor/Ministry/Framework rows, so a
    /// filter would silently store one ministry's figures as the national ones.
    /// </summary>
    public interface IMinistryScopeService
    {
        /// <summary>Resolved once per request.</summary>
        Task<MinistryScope> GetScopeAsync(CancellationToken cancellationToken = default);

        // Each returns true for an administrator without querying. For anyone else it returns
        // false both for a row outside their ministry and for a row that does not exist, so
        // callers that want a missing id to stay NotFound must load the row first.
        Task<bool> CanAccessMinistryAsync(int ministryCode, CancellationToken cancellationToken = default);
        Task<bool> CanAccessProjectAsync(int projectId, CancellationToken cancellationToken = default);
        Task<bool> CanAccessPhaseAsync(int phaseId, CancellationToken cancellationToken = default);
        Task<bool> CanAccessPhasesAsync(IReadOnlyCollection<int> phaseIds, CancellationToken cancellationToken = default);
        Task<bool> CanAccessMeasureAsync(int measureCode, CancellationToken cancellationToken = default);
        Task<bool> CanAccessActionPlanAsync(int actionPlanCode, CancellationToken cancellationToken = default);
        Task<bool> CanAccessPlansAsync(IReadOnlyCollection<int> planCodes, CancellationToken cancellationToken = default);
        Task<bool> CanAccessFrameworkAsync(int frameworkCode, CancellationToken cancellationToken = default);
        Task<bool> CanAccessOutcomeAsync(int outcomeCode, CancellationToken cancellationToken = default);
        Task<bool> CanAccessOutputAsync(int outputCode, CancellationToken cancellationToken = default);
        Task<bool> CanAccessSubOutputAsync(int subOutputCode, CancellationToken cancellationToken = default);

        /// <summary>By the strategy the indicator sits under, not the project it measures.</summary>
        Task<bool> CanAccessIndicatorAsync(int indicatorCode, CancellationToken cancellationToken = default);
    }
}
