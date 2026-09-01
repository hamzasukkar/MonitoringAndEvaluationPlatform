using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A named grouping of <see cref="ImpactIndicator"/>s drawn from across projects, tagged with
    /// the ministries and frameworks it serves — the unit the top-level Impact page lists.
    ///
    /// This is deliberately NOT <see cref="Output"/>, which is a level of the results framework
    /// (Framework → Outcome → Output → SubOutput → Indicator) and carries a weight that rolls up
    /// into framework performance. A ProjectOutput sits on the parallel impact track: it groups
    /// what projects actually delivered, and feeds nothing back into *Performance columns.
    ///
    /// Every PERCENTAGE on the Impact page is rolled up live from the linked indicators, so those
    /// figures can never drift out of sync with the underlying data. The only numbers stored on
    /// the output itself are BaseValue/TargetValue, which are inputs the user supplies rather
    /// than derived values, and so cannot drift either.
    /// </summary>
    public class ProjectOutput
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Project output name is required.")]
        [StringLength(300, MinimumLength = 2, ErrorMessage = "Project output name must be between 2 and 300 characters.")]
        [Display(Name = "Development Impact Indicator Name")]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Starting point this output is measured from, in its own real units. Nullable: an
        /// output created before its bounds were known has no base, and a stored 0 would make
        /// every Amount of Change read 0.00 as if it were real data.
        /// </summary>
        [Display(Name = "Base Value")]
        public double? BaseValue { get; set; }

        /// <summary>
        /// The value this output aims to reach, in its own real units.
        ///
        /// Deliberately NOT the same as <see cref="TotalTarget"/>, which is the computed sum of
        /// the linked indicators' own TargetValues. This one is entered by hand for the output as
        /// a whole and, together with <see cref="BaseValue"/>, defines the gap that
        /// <see cref="GetAmountOfChangeForYear"/> measures progress across.
        /// </summary>
        [Display(Name = "Target")]
        public double? TargetValue { get; set; }

        // Ministries/Frameworks are plain many-to-many skip navigations; join tables are named
        // in ApplicationDbContext. ImpactIndicators is a link WITH A PAYLOAD (each link carries a
        // user-assigned Weight), so it is an explicit join entity rather than a skip navigation —
        // IndicatorLinks is the mapped source of truth; ImpactIndicators below is a read-only
        // projection over it, kept only so every existing .Sum()/.Where()/.Any() reader and every
        // view that expects IEnumerable<ImpactIndicator> keeps compiling unchanged. Unlike
        // Project.Donors/ProjectDonors (two independently-mapped collections the controller must
        // keep in sync by hand), this projection can never drift — there is nothing to sync.
        public ICollection<Ministry> Ministries { get; set; } = new List<Ministry>();
        public ICollection<Framework> Frameworks { get; set; } = new List<Framework>();
        public ICollection<ProjectOutputImpactIndicator> IndicatorLinks { get; set; } = new List<ProjectOutputImpactIndicator>();

        /// <summary>
        /// Hand-recorded actual impact, one row per year. Independent of every computed figure
        /// here — see ProjectOutputActualImpact.
        /// </summary>
        public ICollection<ProjectOutputActualImpact> ActualImpacts { get; set; } = new List<ProjectOutputActualImpact>();

        [NotMapped]
        public IEnumerable<ImpactIndicator> ImpactIndicators =>
            IndicatorLinks?.Select(l => l.ImpactIndicator) ?? Enumerable.Empty<ImpactIndicator>();

        // ─────────────────────────── Rolled-up figures ───────────────────────────
        // Every member below needs the query to have loaded
        //   .Include(po => po.IndicatorLinks).ThenInclude(l => l.ImpactIndicator).ThenInclude(i => i.YearlyValues)
        //   .Include(po => po.IndicatorLinks).ThenInclude(l => l.ImpactIndicator).ThenInclude(i => i.Project)
        // A missing Include renders an empty row rather than throwing, so it fails quietly.

        /// <summary>
        /// The years this output spans — the union of the covered years of every linked
        /// indicator's project. Empty when nothing is linked yet.
        /// </summary>
        [NotMapped]
        public IEnumerable<int> CoveredYears =>
            ImpactIndicators == null
                ? Enumerable.Empty<int>()
                : ImpactIndicators
                    .Where(i => i.Project != null)
                    .SelectMany(i => i.Project.CoveredYears)
                    .Distinct()
                    .OrderBy(y => y);

        /// <summary>
        /// The year BaseValue is measured at: the first year this output covers. Derived rather
        /// than stored — CoveredYears is already ordered ascending. Null when nothing is linked
        /// yet, in which case there is no base year to speak of.
        ///
        /// Being derived, this moves if the underlying projects' dates move. That is the
        /// trade-off the user chose over entering a base year by hand.
        /// </summary>
        [NotMapped]
        public int? BaseYear => CoveredYears.Any() ? CoveredYears.First() : null;

        /// <summary>
        /// Sum of that year's raw values across every linked indicator. Still useful for a raw
        /// quantity figure (e.g. a tooltip), but no longer what drives the displayed percentages —
        /// see GetWeightedPercentageForYear.
        /// </summary>
        public double GetValueForYear(int year) =>
            ImpactIndicators?.Sum(i => i.GetValueForYear(year)) ?? 0;

        /// <summary>
        /// True when at least one linked indicator actually recorded that year. Lets the table
        /// show "—" for an unmeasured year instead of a misleading 0.00.
        /// </summary>
        public bool HasValueForYear(int year) =>
            ImpactIndicators?.Any(i => i.HasValueForYear(year)) ?? false;

        [NotMapped]
        public double TotalTarget => ImpactIndicators?.Sum(i => i.TargetValue) ?? 0;

        [NotMapped]
        public double TotalAchieved => ImpactIndicators?.Sum(i => i.AchievedValue) ?? 0;

        /// <summary>
        /// Ratio of raw summed quantities. Kept for reference/tooltips, but superseded by
        /// WeightedAchievementRate for anything displayed as "the" achievement rate — summing raw
        /// quantities across indicators with different units (wells vs. students) is not strictly
        /// meaningful; averaging each indicator's own rate is.
        /// </summary>
        [NotMapped]
        public double AchievementRate => TotalTarget > 0 ? TotalAchieved / TotalTarget * 100 : 0;

        /// <summary>
        /// Weighted average of AchievementRate across every linked indicator, using the weight the
        /// user assigned to each link. Same formula, including its equal-weight fallback, as
        /// PerformanceService.CalculateWeightedPerformance — this is the results-framework
        /// hierarchy's established way of combining child rates into a parent rate, applied here
        /// to indicators instead of SubOutputs/Outputs/Outcomes.
        /// </summary>
        [NotMapped]
        public double WeightedAchievementRate
        {
            get
            {
                if (IndicatorLinks == null || !IndicatorLinks.Any()) return 0;

                double totalWeight = IndicatorLinks.Sum(l => l.Weight);
                if (totalWeight <= 0) totalWeight = IndicatorLinks.Count;

                return IndicatorLinks.Sum(l => l.ImpactIndicator.AchievementRate * l.Weight) / totalWeight;
            }
        }

        /// <summary>
        /// Weighted average, for one year, of each CONTRIBUTING indicator's own cumulative
        /// percentage as of that year (see ImpactIndicator.GetCumulativePercentageForYear). An
        /// indicator whose own project does not cover the given year contributes neither its
        /// value nor its weight that year — same range exclusion already applied to decide
        /// whether a per-year cell shows a number or a dash.
        /// </summary>
        public double GetWeightedPercentageForYear(int year)
        {
            var contributing = IndicatorLinks?
                .Where(l => l.ImpactIndicator.Project != null && l.ImpactIndicator.Project.CoveredYears.Contains(year))
                .ToList();

            if (contributing == null || !contributing.Any()) return 0;

            double totalWeight = contributing.Sum(l => l.Weight);
            if (totalWeight <= 0) totalWeight = contributing.Count;

            return contributing.Sum(l => l.ImpactIndicator.GetCumulativePercentageForYear(year) * l.Weight) / totalWeight;
        }

        /// <summary>
        /// How much of the BaseValue-to-TargetValue gap has been closed as of this year, in the
        /// output's own real units: ((TargetValue - BaseValue) * ActualValue%) / 100, where
        /// ActualValue% is GetWeightedPercentageForYear.
        ///
        /// This is the algebraic inverse of FrameworkGoal.ProgressRate, which computes
        /// ((Current - Base) / (Target - Base)) * 100 from the same three quantities.
        ///
        /// NOTE: this is the CHANGE from the baseline, not the current level. With Base 20,
        /// Target 100 and 87.5% actual, this returns 70 (the gap closed) while the current level
        /// would be 90 (= BaseValue + 70). Named for FrameworkGoal.cs, which already documents
        /// this exact concept as "Amount of Change".
        ///
        /// A negative result is legitimate and intentional: a decrease goal (Target below Base)
        /// is a normal case the framework goals already support.
        ///
        /// Null when either bound is unset, so the caller can render an em dash rather than a
        /// misleading 0.00 - the same "no data is not zero" rule used throughout this feature.
        /// </summary>
        public double? GetAmountOfChangeForYear(int year)
        {
            if (BaseValue is null || TargetValue is null) return null;

            return ((TargetValue.Value - BaseValue.Value) * GetWeightedPercentageForYear(year)) / 100;
        }

        /// <summary>
        /// BaseValue + GetAmountOfChangeForYear(year) — the level this output has reached as of
        /// that year, in its own real units. Labelled "Total Value" on the Impact table.
        ///
        /// Null when either bound is unset, so the caller renders a dash. Returning a bare
        /// BaseValue in that case would look like real progress had been measured when none has.
        /// </summary>
        public double? GetTotalValueForYear(int year)
        {
            var change = GetAmountOfChangeForYear(year);
            if (change is null || BaseValue is null) return null;

            return BaseValue.Value + change.Value;
        }

        /// <summary>
        /// The impact actually recorded by hand for this year, or null when none was entered.
        /// Null is meaningfully different from a recorded 0, so callers must not coalesce it.
        ///
        /// Needs .Include(po => po.ActualImpacts) on the query, or every year reads as unrecorded.
        /// </summary>
        public double? GetActualImpactForYear(int year) =>
            ActualImpacts?.FirstOrDefault(a => a.Year == year)?.Value;
    }
}
