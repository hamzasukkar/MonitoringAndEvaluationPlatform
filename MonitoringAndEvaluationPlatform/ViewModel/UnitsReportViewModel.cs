using Microsoft.AspNetCore.Mvc.Rendering;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    /// <summary>
    /// The Units report: everything the system measures in a unit of measurement, grouped by that
    /// unit and broken down by project and project date range.
    ///
    /// Two parallel tracks feed it, because units are attached to two different things that both
    /// belong to a project:
    ///   • Impact indicators — target vs. the sum of their yearly values.
    ///   • Phase measures    — the phase's target quantity vs. the latest measure recorded for it.
    ///
    /// Totals are only ever summed WITHIN a unit. Adding "12 km" to "4 schools" would be a
    /// meaningless number, so there is deliberately no cross-unit quantity total anywhere here —
    /// the cross-unit figures are counts and percentages only.
    /// </summary>
    public class UnitsReportViewModel
    {
        public UnitsReportFilterViewModel Filter { get; set; } = new();

        public List<SelectListItem> UnitOptions { get; set; } = new();
        public List<SelectListItem> MinistryOptions { get; set; } = new();
        public List<SelectListItem> ProjectOptions { get; set; } = new();

        /// <summary>Impact indicators grouped by unit, largest group first.</summary>
        public List<UnitReportGroup> ImpactGroups { get; set; } = new();

        /// <summary>Project phases (via their latest measure) grouped by unit, largest group first.</summary>
        public List<UnitReportGroup> MeasureGroups { get; set; } = new();

        public IEnumerable<UnitReportGroup> AllGroups => ImpactGroups.Concat(MeasureGroups);

        /// <summary>Distinct units actually present in the filtered result — not the size of the units list.</summary>
        public int UnitCount => AllGroups.Select(g => g.UnitKey).Distinct().Count();

        public int ProjectCount => AllGroups
            .SelectMany(g => g.Rows)
            .Select(r => r.ProjectId)
            .Distinct()
            .Count();

        public int ImpactRowCount => ImpactGroups.Sum(g => g.Rows.Count);
        public int MeasureRowCount => MeasureGroups.Sum(g => g.Rows.Count);

        /// <summary>
        /// Average achievement across every row that has a target to measure against. A percentage,
        /// so unlike the raw quantities it is comparable across units.
        /// </summary>
        public double AverageAchievement
        {
            get
            {
                var rated = AllGroups.SelectMany(g => g.Rows).Where(r => r.Target > 0).ToList();
                return rated.Count > 0 ? Math.Round(rated.Average(r => r.AchievementRate), 1) : 0;
            }
        }

        public bool HasAnyData => ImpactGroups.Count > 0 || MeasureGroups.Count > 0;
    }

    /// <summary>
    /// Filter state, round-tripped through the query string so a filtered report can be bookmarked
    /// and shared. Every field is optional; null means "no restriction".
    /// </summary>
    public class UnitsReportFilterViewModel
    {
        /// <summary>MeasurementUnit.Code, or <see cref="UnitReportGroup.UnspecifiedUnitKey"/> for the "no unit" group.</summary>
        public int? UnitCode { get; set; }

        public int? MinistryCode { get; set; }

        public int? ProjectId { get; set; }

        /// <summary>
        /// Date range matched by OVERLAP, not containment: a project is included when it was active
        /// at any point in the range. A project running 2023–2027 therefore shows up for a 2025
        /// range, which is what "projects in this period" is normally taken to mean.
        /// </summary>
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool IsActive =>
            UnitCode.HasValue || MinistryCode.HasValue || ProjectId.HasValue ||
            FromDate.HasValue || ToDate.HasValue;
    }

    /// <summary>One unit of measurement and every project row measured in it.</summary>
    public class UnitReportGroup
    {
        /// <summary>Stands in for UnitCode on rows whose unit was never set, so they are reported rather than dropped.</summary>
        public const int UnspecifiedUnitKey = -1;

        public int UnitKey { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public bool IsUnspecified => UnitKey == UnspecifiedUnitKey;

        public List<UnitReportRow> Rows { get; set; } = new();

        public int ProjectCount => Rows.Select(r => r.ProjectId).Distinct().Count();

        /// <summary>Safe to sum: every row in a group shares the same unit.</summary>
        public double TotalTarget => Rows.Sum(r => r.Target);
        public double TotalAchieved => Rows.Sum(r => r.Achieved);

        public double AchievementRate => TotalTarget > 0
            ? Math.Round(TotalAchieved / TotalTarget * 100, 1)
            : 0;

        public double RemainingToTarget => Math.Max(TotalTarget - TotalAchieved, 0);

        /// <summary>The span the group's projects collectively cover — null when no row carries dates.</summary>
        public DateTime? EarliestStart => Rows.Count > 0 ? Rows.Min(r => r.ProjectStart) : null;
        public DateTime? LatestEnd => Rows.Count > 0 ? Rows.Max(r => r.ProjectEnd) : null;
    }

    /// <summary>
    /// One measured line in the report: an impact indicator, or a project phase represented by its
    /// most recent measure. Both carry the owning project and that project's date range.
    /// </summary>
    public class UnitReportRow
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string MinistryName { get; set; } = string.Empty;

        /// <summary>Indicator name, or phase name on the measures track.</summary>
        public string ItemName { get; set; } = string.Empty;

        public double Target { get; set; }
        public double Achieved { get; set; }

        public DateTime ProjectStart { get; set; }
        public DateTime ProjectEnd { get; set; }

        /// <summary>Measures track only: when the latest measure feeding this row was recorded.</summary>
        public DateTime? LastRecorded { get; set; }

        /// <summary>Uncapped, matching ImpactIndicator.AchievementRate — overshoot is information.</summary>
        public double AchievementRate => Target > 0
            ? Math.Round(Achieved / Target * 100, 1)
            : 0;

        public bool HasTarget => Target > 0;

        /// <summary>True once the project's end date has passed but its target has not been met.</summary>
        public bool IsOverdue => ProjectEnd < DateTime.Today && Achieved < Target;
    }
}
