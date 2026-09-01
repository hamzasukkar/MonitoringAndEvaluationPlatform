using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A quantitative impact indicator owned by a single project — "120 schools rehabilitated".
    ///
    /// This is deliberately NOT the same thing as <see cref="Indicator"/>, which belongs to the
    /// results framework (SubOutput → Output → Outcome → Framework) and whose IndicatorsPerformance
    /// is a copy of the project's execution performance.
    ///
    /// Impact indicators are a parallel track: nothing here feeds Project.performance,
    /// DisbursementPerformance, or the framework roll-up. They measure what the project delivered,
    /// not how far along it is.
    ///
    /// Achievement is CUMULATIVE: the yearly values sum to the achieved total, which is then
    /// compared against <see cref="TargetValue"/>. The set of years comes from the parent project's
    /// StartDate/EndDate — see <see cref="Project.CoveredYears"/> — and is never stored here.
    /// </summary>
    public class ImpactIndicator
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Indicator name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Indicator name must be between 2 and 200 characters.")]
        [Display(Name = "Indicator Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Unit of measurement (school, km, beneficiary, %). Without it the raw target and
        /// achieved numbers are meaningless in tables and reports.
        ///
        /// Chosen from the system-wide Units list rather than typed; <see cref="Unit"/> keeps
        /// the old string shape so every reader and report is unaffected by that change.
        /// </summary>
        [Display(Name = "Unit")]
        public int? UnitCode { get; set; }

        // BindNever: MeasuresController binds a whole entity straight from the form, and a
        // posted UnitRef would be inserted as a new unit on save.
        [BindNever]
        [ForeignKey(nameof(UnitCode))]
        public virtual MeasurementUnit? UnitRef { get; set; }

        /// <summary>
        /// The unit's name in the current culture, or null when no unit is set. Auto-included by
        /// the model configuration, so this never silently reads null on a loaded entity.
        /// </summary>
        [NotMapped]
        [Display(Name = "Unit")]
        public string? Unit => UnitRef?.DisplayName;

        [Required(ErrorMessage = "Target value is required.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Target value must be greater than zero.")]
        [Display(Name = "Target Value")]
        public double TargetValue { get; set; }

        public int ProjectID { get; set; }
        public virtual Project Project { get; set; } = null!;

        /// <summary>
        /// One row per year that has actually been entered. A year with no row means "not yet
        /// entered", which is deliberately distinct from a year recorded as zero.
        /// </summary>
        public virtual ICollection<ImpactIndicatorYearlyValue> YearlyValues { get; set; }
            = new List<ImpactIndicatorYearlyValue>();

        /// <summary>
        /// Links to the project outputs that group this indicator on the top-level Impact page,
        /// each carrying the weight that output assigned to this indicator. Many-to-many via an
        /// explicit join entity (not a plain skip navigation) because the link itself has a
        /// payload — see ProjectOutputImpactIndicator.
        /// </summary>
        public virtual ICollection<ProjectOutputImpactIndicator> ProjectOutputLinks { get; set; }
            = new List<ProjectOutputImpactIndicator>();

        /// <summary>
        /// Total delivered so far — the sum of every yearly value. Computed, never stored, so it
        /// can never drift out of sync with the yearly rows.
        ///
        /// Any query that renders this MUST .Include(i => i.YearlyValues); an un-included load
        /// silently reports zero rather than failing.
        /// </summary>
        [NotMapped]
        [Display(Name = "Achieved Value")]
        public double AchievedValue =>
            YearlyValues?.Sum(v => v.Value) ?? 0;

        /// <summary>
        /// Achievement as a percentage of the target. Deliberately uncapped: exceeding the target
        /// shows as more than 100% rather than being clamped, because overshoot is information.
        /// </summary>
        [NotMapped]
        [Display(Name = "Achievement Rate")]
        public double AchievementRate =>
            TargetValue > 0 ? AchievedValue / TargetValue * 100 : 0;

        /// <summary>Remaining amount to reach the target; zero once the target is met or exceeded.</summary>
        [NotMapped]
        public double RemainingToTarget => Math.Max(TargetValue - AchievedValue, 0);

        /// <summary>
        /// Value recorded for a given year, or 0 when that year has no row.
        /// Mirrors FrameworkGoal.GetBaseValueForYear.
        /// </summary>
        public double GetValueForYear(int year) =>
            YearlyValues?.FirstOrDefault(v => v.Year == year)?.Value ?? 0;

        /// <summary>
        /// True when a row exists for the year — lets views distinguish "entered as 0" from
        /// "not entered yet", which <see cref="GetValueForYear"/> alone cannot.
        /// </summary>
        public bool HasValueForYear(int year) =>
            YearlyValues?.Any(v => v.Year == year) ?? false;

        /// <summary>
        /// This indicator's own cumulative achievement, as a percentage of its own target, as of
        /// the given year (sum of every recorded value in years up to and including it, divided
        /// by TargetValue). ImpactIndicatorsController.ApplyYearValues never writes a YearlyValue
        /// row outside this indicator's own project range, so a plain Year &lt;= year filter
        /// reproduces the same running total a year-by-year loop would build — no explicit range
        /// gate needed here; callers still gate on Project.CoveredYears for the DISPLAY decision
        /// (dash vs. number), since a year genuinely outside range should show neither.
        /// </summary>
        public double GetCumulativePercentageForYear(int year) =>
            TargetValue > 0
                ? (YearlyValues?.Where(v => v.Year <= year).Sum(v => v.Value) ?? 0) / TargetValue * 100
                : 0;

        /// <summary>
        /// Stored values whose year falls outside the project's current date range — created when
        /// someone shortens the project after data was entered. These are never auto-deleted; the
        /// details view surfaces them so the data does not silently vanish.
        /// </summary>
        public IEnumerable<ImpactIndicatorYearlyValue> ValuesOutsideProjectRange(IEnumerable<int> coveredYears)
        {
            if (YearlyValues == null) return Enumerable.Empty<ImpactIndicatorYearlyValue>();

            var years = coveredYears as ISet<int> ?? new HashSet<int>(coveredYears);
            return YearlyValues.Where(v => !years.Contains(v.Year)).OrderBy(v => v.Year);
        }
    }
}
