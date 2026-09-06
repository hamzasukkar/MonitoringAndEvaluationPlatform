using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// Everything the platform knows about one ministry, rolled up across the four things a
    /// ministry is measured on: performance, money, size, and delivered impact.
    ///
    /// Built by <see cref="IMinistryStatisticsService"/> so the dashboard tier and the ministry
    /// report cannot drift apart — two pages showing different numbers for the same ministry is
    /// the failure this type exists to prevent.
    ///
    /// Display names are carried in both languages rather than resolved here: picking one is a
    /// presentation decision, and a service that reads CurrentUICulture cannot be used from a
    /// background job or tested without a culture scope.
    /// </summary>
    public sealed class MinistryStatistics
    {
        public int Code { get; init; }
        public string DisplayNameAr { get; init; } = string.Empty;
        public string DisplayNameEn { get; init; } = string.Empty;

        /// <summary>An emoji, not a file path — see <see cref="Ministry.Logo"/>.</summary>
        public string? Logo { get; init; }

        // ───────────────────────────── Performance ─────────────────────────────
        // Two legitimate answers to "how is this ministry doing", deliberately both reported.
        // They differ whenever a ministry owns strategies whose indicators point at another
        // ministry's projects, or owns projects that none of its strategies covers. Showing one
        // alone would silently contradict the other page that shows the other one.

        /// <summary>
        /// The stored <see cref="Ministry.IndicatorsPerformance"/> — an unweighted mean over the
        /// ministry's projects, maintained by MonitoringService.
        /// </summary>
        public double ProjectAverageIndicators { get; init; }

        /// <summary>The stored <see cref="Ministry.DisbursementPerformance"/>, same basis.</summary>
        public double ProjectAverageDisbursement { get; init; }

        /// <summary>
        /// Mean of <see cref="Framework.IndicatorsPerformance"/> across the strategies this
        /// ministry owns. Null — not zero — when it owns none, so the view can show "—" instead
        /// of a 0% that reads as failure.
        /// </summary>
        public double? StrategyRollupIndicators { get; init; }

        /// <summary>Mean of <see cref="Framework.DisbursementPerformance"/>, same basis.</summary>
        public double? StrategyRollupDisbursement { get; init; }

        // ───────────────────────────── Financial ─────────────────────────────

        /// <summary>Estimated budget in SYP, carrying whatever could not be converted.</summary>
        public MoneyTotal Budget { get; init; } = MoneyTotal.Empty;

        /// <summary>Sum of every phase plan's Realised value, in SYP.</summary>
        public MoneyTotal Disbursed { get; init; } = MoneyTotal.Empty;

        /// <summary>
        /// Disbursed as a percentage of budget. Both sides are SYP totals over the same project
        /// set, so a project excluded from one is excluded from the other and the ratio stays
        /// honest even when <see cref="Budget"/> is incomplete.
        /// </summary>
        public double SpendRate => Budget.Syp > 0
            ? Math.Round(Disbursed.Syp / Budget.Syp * 100, 2)
            : 0;

        // ─────────────────────────────── Counts ───────────────────────────────

        /// <summary>Strategies (frameworks) this ministry owns via Framework.MinistryCode.</summary>
        public int StrategyCount { get; init; }

        /// <summary>Results-framework indicators reachable under those strategies.</summary>
        public int IndicatorCount { get; init; }

        public int ProjectCount { get; init; }

        /// <summary>Still running — end date today or later, matching MinistriesController.</summary>
        public int ActiveProjectCount { get; init; }
        public int CompletedProjectCount { get; init; }

        // ─────────────────────────────── Impact ───────────────────────────────

        public int ImpactOutputCount { get; init; }
        public int ImpactIndicatorCount { get; init; }

        /// <summary>
        /// Mean of <see cref="ProjectOutput.WeightedAchievementRate"/> across this ministry's
        /// outputs that actually have indicators linked. Null when it has none — impact is a
        /// parallel track that feeds nothing into the performance columns, so "no impact data"
        /// and "0% achieved" are genuinely different states.
        /// </summary>
        public double? ImpactWeightedAchievement { get; init; }

        // ───────────────────────────── Diagnostic ─────────────────────────────

        /// <summary>
        /// Projects counted here that are attached to this ministry by only ONE of the two edges
        /// (the Project.MinistryCode FK, or the ProjectMinistries many-to-many). The two are
        /// hand-synced by ProjectsController, so they can diverge on legacy rows; reporting the
        /// count makes that visible rather than letting the totals quietly disagree with a page
        /// that reads the other edge.
        /// </summary>
        public int LinkageMismatchCount { get; init; }

        public string DisplayName(bool arabic) =>
            arabic
                ? (string.IsNullOrWhiteSpace(DisplayNameAr) ? DisplayNameEn : DisplayNameAr)
                : (string.IsNullOrWhiteSpace(DisplayNameEn) ? DisplayNameAr : DisplayNameEn);
    }

    public interface IMinistryStatisticsService
    {
        /// <summary>
        /// Rolls up statistics for one ministry or all of them.
        /// </summary>
        /// <param name="ministryCode">
        /// Null for every ministry; a value for just that one. Callers are responsible for
        /// scoping — pass the caller's own ministry code for a non-admin.
        /// </param>
        /// <param name="fromDate">
        /// Optional range, matched by OVERLAP rather than containment: a project active at any
        /// point in the range is included, the same rule the Units report uses.
        /// </param>
        Task<IReadOnlyList<MinistryStatistics>> GetAsync(
            int? ministryCode = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The projects behind <see cref="MinistryStatistics"/>, keyed by ministry code — the row
        /// detail for a report. Same membership rule and date filter as <see cref="GetAsync"/>,
        /// so the rows always add up to the totals.
        ///
        /// Takes every ministry at once rather than one at a time: a project can belong to two
        /// ministries, so a per-ministry call would re-query the same rows N times.
        /// </summary>
        Task<IReadOnlyDictionary<int, IReadOnlyList<Project>>> GetProjectsByMinistryAsync(
            IEnumerable<int> ministryCodes,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);
    }
}
