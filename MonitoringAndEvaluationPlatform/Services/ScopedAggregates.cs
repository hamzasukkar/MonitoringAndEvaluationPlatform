using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// Recomputes a category's performance from an arbitrary set of projects, for users who may
    /// only see their own ministry. The stored IndicatorsPerformance / DisbursementPerformance
    /// columns on Sector, Donor and Ministry are national figures built from every ministry's
    /// projects, so showing them to a ministry user would leak other ministries' results.
    ///
    /// The formulas are the ones MonitoringService stores, so an administrator's stored figure and
    /// a ministry user's recomputed one mean the same thing:
    ///   indicators   = unweighted mean of Project.performance (0 when there are no projects);
    ///   disbursement = Σ realised ÷ Σ estimated budget × 100, both converted to SYP, with a project
    ///                  that cannot be converted dropped from BOTH sums.
    /// The disbursement figure needs Phases → ActionPlan → Plans loaded on each project.
    /// </summary>
    public static class ScopedAggregates
    {
        public static double IndicatorsPerformance(IReadOnlyCollection<Project> projects) =>
            projects.Count > 0 ? projects.Average(p => p.performance) : 0;

        public static double DisbursementPerformance(IReadOnlyCollection<Project> projects, CurrencyConverter converter)
        {
            var budget = converter.SumBudget(projects).Syp;
            if (budget <= 0) return 0;
            return converter.Sum(projects, MinistryStatisticsService.RealisedOf).Syp / budget * 100;
        }
    }
}
