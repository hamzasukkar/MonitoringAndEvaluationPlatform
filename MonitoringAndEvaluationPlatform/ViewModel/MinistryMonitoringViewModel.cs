using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    /// <summary>
    /// One ministry card on the Monitoring ministry level — the metrics from
    /// <see cref="MinistryStatistics"/> plus the hierarchy counts the navigation grid needs.
    ///
    /// The counts live here rather than on <see cref="MinistryStatistics"/> because they are
    /// Monitoring-specific: each one must be computed on exactly the predicate the nav card's
    /// LINK uses, so the number on the card equals the number of rows you land on. Pushing that
    /// rule into the shared DTO would make every Dashboard tier poll and every Ministry Report
    /// render pay for counts neither of them displays.
    /// </summary>
    public sealed class MinistryMonitoringCard
    {
        public required MinistryStatistics Stats { get; init; }

        // Counted down the strategy-ownership path (Framework.MinistryCode), matching the
        // Outcome / Outputs / SubOutputs links.
        public int OutcomeCount { get; init; }
        public int OutputCount { get; init; }
        public int SubOutputCount { get; init; }

        // Counted down the project-ownership path (either ministry edge), matching the Phase link
        // and Stats.ProjectCount.
        public int PhaseCount { get; init; }

        /// <summary>
        /// The headline indicators figure: the mean of <c>Framework.IndicatorsPerformance</c> across
        /// the ministry's strategies — deliberately NOT the stored <c>Ministry.IndicatorsPerformance</c>,
        /// which averages over projects instead. This card sits directly above the framework cards,
        /// so its number has to be the mean of the numbers printed on them; the two differ enough in
        /// practice that showing the project average here reads as a bug.
        ///
        /// Null when the ministry owns no strategies — there is no roll-up to show, and 0% would
        /// read as failure.
        /// </summary>
        public double? IndicatorsPerformance => Stats.StrategyRollupIndicators;

        public double? DisbursementPerformance => Stats.StrategyRollupDisbursement;

        /// <summary>
        /// What the client-side performance filter sorts and buckets on. The displayed value can be
        /// an em dash, and <c>parseFloat("—")</c> is NaN, which would silently drop the card from
        /// every filter — so the card carries a numeric attribute alongside the display text.
        /// </summary>
        public double PerformanceValue => IndicatorsPerformance ?? 0;
    }

    public sealed class MinistryMonitoringViewModel
    {
        public IReadOnlyList<MinistryMonitoringCard> Cards { get; init; } = Array.Empty<MinistryMonitoringCard>();
    }
}
