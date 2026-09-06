using Microsoft.AspNetCore.Mvc.Rendering;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    /// <summary>
    /// The Ministry report: every ministry's scorecard, grouped by ministry and broken down by
    /// the projects behind it.
    ///
    /// The per-ministry metrics are <see cref="MinistryStatistics"/> straight from
    /// <see cref="IMinistryStatisticsService"/> rather than a parallel view model, so this page
    /// and the dashboard's ministry tier cannot show different numbers for the same ministry.
    /// </summary>
    public class MinistryReportViewModel
    {
        public MinistryReportFilterViewModel Filter { get; set; } = new();

        /// <summary>Populated for administrators only; empty hides the control for a ministry user.</summary>
        public List<SelectListItem> MinistryOptions { get; set; } = new();

        public List<MinistryReportGroup> Groups { get; set; } = new();

        public bool HasAnyData => Groups.Count > 0;

        public int MinistryCount => Groups.Count;

        public int ProjectCount => Groups.Sum(g => g.Stats.ProjectCount);

        public int StrategyCount => Groups.Sum(g => g.Stats.StrategyCount);

        /// <summary>
        /// Portfolio budget across the listed ministries. Summed as SYP figures with their
        /// unconverted counts carried forward, so the page-level total is exactly as complete
        /// as the rows that feed it.
        /// </summary>
        public MoneyTotal TotalBudget => Combine(Groups.Select(g => g.Stats.Budget));

        public MoneyTotal TotalDisbursed => Combine(Groups.Select(g => g.Stats.Disbursed));

        /// <summary>
        /// Mean of the stored project-average performance. Unweighted, matching how the stored
        /// ministry figure itself is built — a budget weighting here would be a third definition.
        /// </summary>
        public double AverageProjectPerformance => Groups.Count > 0
            ? Math.Round(Groups.Average(g => g.Stats.ProjectAverageIndicators), 1)
            : 0;

        /// <summary>Mean of the strategy roll-up, over the ministries that own strategies.</summary>
        public double? AverageStrategyPerformance
        {
            get
            {
                var rated = Groups
                    .Where(g => g.Stats.StrategyRollupIndicators.HasValue)
                    .ToList();

                return rated.Count > 0
                    ? Math.Round(rated.Average(g => g.Stats.StrategyRollupIndicators!.Value), 1)
                    : null;
            }
        }

        /// <summary>Ministries whose two ministry edges disagree on at least one project.</summary>
        public int MinistriesWithLinkageMismatch =>
            Groups.Count(g => g.Stats.LinkageMismatchCount > 0);

        private static MoneyTotal Combine(IEnumerable<MoneyTotal> totals)
        {
            double syp = 0;
            var count = 0;
            var byCurrency = new Dictionary<string, double>();

            foreach (var total in totals)
            {
                syp += total.Syp;
                count += total.UnconvertedCount;
                foreach (var pair in total.UnconvertedByCurrency)
                {
                    byCurrency[pair.Key] = byCurrency.GetValueOrDefault(pair.Key) + pair.Value;
                }
            }

            return new MoneyTotal(syp, count, byCurrency);
        }
    }

    /// <summary>
    /// Filter state, round-tripped through the query string so a filtered report can be
    /// bookmarked and shared. Every field is optional; null means "no restriction".
    /// </summary>
    public class MinistryReportFilterViewModel
    {
        public int? MinistryCode { get; set; }

        /// <summary>
        /// Matched by OVERLAP, not containment: a project running 2023–2027 belongs in a 2025
        /// report. Same rule as the Units report.
        /// </summary>
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool IsActive =>
            MinistryCode.HasValue || FromDate.HasValue || ToDate.HasValue;
    }

    /// <summary>One ministry's scorecard and the projects behind it.</summary>
    public class MinistryReportGroup
    {
        public MinistryStatistics Stats { get; set; } = new();

        public List<MinistryReportRow> Rows { get; set; } = new();
    }

    /// <summary>One project counted under a ministry.</summary>
    public class MinistryReportRow
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string SectorName { get; set; } = string.Empty;

        /// <summary>Estimated budget in SYP, or null when it could not be converted.</summary>
        public double? BudgetSyp { get; set; }

        /// <summary>Sum of the project's plan Realised values in SYP, or null when unconvertible.</summary>
        public double? DisbursedSyp { get; set; }

        public double Performance { get; set; }
        public double DisbursementPerformance { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsCompleted => EndDate < DateTime.Today;

        /// <summary>
        /// True when only one of the two ministry edges names this ministry. Surfaced per row so
        /// the reader can tell exactly which project is responsible for a group's mismatch count.
        /// </summary>
        public bool LinkageMismatch { get; set; }

        public double SpendRate => BudgetSyp is > 0 && DisbursedSyp.HasValue
            ? Math.Round(DisbursedSyp.Value / BudgetSyp.Value * 100, 1)
            : 0;
    }
}
