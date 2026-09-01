using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class FrameworkGoal
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public int StartingYear { get; set; }

        [Required]
        public double BaseValueForStartingYear { get; set; }

        [Required]
        public int CurrentYear { get; set; }

        [Required]
        public double BaseValueForCurrentYear { get; set; }

        [Required]
        public int TargetYear { get; set; }

        [Required]
        public double TargetValue { get; set; }

        // Foreign key
        public int FrameworkCode { get; set; }

        // Navigation property
        [ForeignKey("FrameworkCode")]
        public Framework Framework { get; set; }

        // Collection of historical yearly values
        public ICollection<FrameworkGoalYearlyValue> YearlyValues { get; set; } = new List<FrameworkGoalYearlyValue>();

        // Notes field for additional information
        public string Notes { get; set; } = string.Empty;

        // When true, expected target values for intermediate years are set manually at creation
        // and will not be auto-calculated. This flag is permanent and cannot be changed after creation.
        public bool ManualYearlyTargets { get; set; } = false;

        // Qualitative: values are percentages. Quantitative: values are quantities expressed in Unit.
        // Set at creation only; cannot be changed afterwards (same contract as ManualYearlyTargets).
        public FrameworkGoalType GoalType { get; set; } = FrameworkGoalType.Qualitative;

        // Unit of measure for Quantitative goals (e.g. "كم", "وحدة سكنية"). Null for Qualitative goals.
        // Chosen from the system-wide Units list; Unit below keeps the old string shape so
        // DisplayUnit, FormatValue and the Excel/PDF exports are unaffected.
        public int? UnitCode { get; set; }

        // BindNever: MeasuresController binds a whole entity straight from the form, and a
        // posted UnitRef would be inserted as a new unit on save.
        [BindNever]
        [ForeignKey(nameof(UnitCode))]
        public virtual MeasurementUnit? UnitRef { get; set; }

        [NotMapped]
        public string? Unit => UnitRef?.DisplayName;

        // Collection of file attachments
        public ICollection<FrameworkGoalFile> Attachments { get; set; } = new List<FrameworkGoalFile>();

        // Collection of manually set expected targets for intermediate years (populated only when ManualYearlyTargets = true)
        public ICollection<FrameworkGoalManualExpectedTarget> ManualExpectedTargets { get; set; } = new List<FrameworkGoalManualExpectedTarget>();

        // For file uploads (not mapped to database)
        [NotMapped]
        public List<IFormFile> UploadedFiles { get; set; } = new List<IFormFile>();

        // Calculated Properties (Not Mapped to Database)

        /// <summary>
        /// True when the goal's values are quantities rather than percentages.
        /// </summary>
        [NotMapped]
        public bool IsQuantitative => GoalType == FrameworkGoalType.Quantitative;

        /// <summary>
        /// Unit shown next to raw values: "%" for qualitative goals, the goal's Unit otherwise.
        /// </summary>
        [NotMapped]
        public string DisplayUnit => IsQuantitative ? (Unit ?? string.Empty) : "%";

        /// <summary>
        /// Formats a raw value together with its unit.
        /// Ratios (ProgressRate, per-year performance) must NOT use this — they are always percentages.
        /// </summary>
        public string FormatValue(double value) => IsQuantitative
            ? $"{value.ToString("N2")} {Unit}".TrimEnd()
            : $"{value.ToString("N2")}%";

        /// <summary>
        /// Determines if this is an increase goal (target > base) or decrease goal (target < base)
        /// </summary>
        [NotMapped]
        public bool IsIncreaseGoal => TargetValue > BaseValueForStartingYear;

        /// <summary>
        /// Annual Change Rate = (Target Value - Base Value for Starting Year) / (Target Year - Starting Year)
        /// Positive for increase goals, negative for decrease goals
        /// </summary>
        [NotMapped]
        public double AnnualChangeRate
        {
            get
            {
                var yearDifference = TargetYear - StartingYear;
                if (yearDifference == 0) return 0;
                return (TargetValue - BaseValueForStartingYear) / yearDifference;
            }
        }

        /// <summary>
        /// Annual Discount Rate = Absolute value of Annual Change Rate (for backward compatibility)
        /// </summary>
        [NotMapped]
        public double AnnualDiscountRate => Math.Abs(AnnualChangeRate);

        /// <summary>
        /// Amount of Change = |Annual Change Rate| × (Current Year - Starting Year)
        /// Always returns positive value representing the magnitude of expected change
        /// </summary>
        [NotMapped]
        public double AmountOfReduction
        {
            get
            {
                var yearsPassed = CurrentYear - StartingYear;
                return Math.Abs(AnnualChangeRate * yearsPassed);
            }
        }

        /// <summary>
        /// Expected Value for Current Year = Base Value + (Annual Change Rate × Years Passed)
        /// Works correctly for both increase and decrease goals
        /// </summary>
        [NotMapped]
        public double ExpectedValueForCurrentYear
        {
            get
            {
                var yearsPassed = CurrentYear - StartingYear;
                return BaseValueForStartingYear + (AnnualChangeRate * yearsPassed);
            }
        }

        /// <summary>
        /// Progress Rate = ((Current Value - Base Value) / (Target Value - Base Value)) × 100
        /// Returns progress as a percentage (0-100)
        /// Works correctly for both increase and decrease goals
        /// </summary>
        [NotMapped]
        public double ProgressRate
        {
            get
            {
                var denominator = TargetValue - BaseValueForStartingYear;
                if (denominator == 0) return 0;

                var numerator = BaseValueForCurrentYear - BaseValueForStartingYear;

                return (numerator / denominator) * 100;
            }
        }

        /// <summary>
        /// Single source of truth for "is this goal on track". Compares actual change to expected
        /// change as a ratio (actualChange / expectedChange), which is sign-safe for both increase
        /// and decrease goals without branching — dividing two same-signed quantities always yields
        /// a comparable positive ratio. Thresholds (0.95 / 0.7) match the tolerance already used to
        /// classify goals for the on-screen status filter.
        /// </summary>
        [NotMapped]
        public FrameworkGoalTrackingStatus TrackingStatus
        {
            get
            {
                var expectedChange = ExpectedValueForCurrentYear - BaseValueForStartingYear;
                var actualChange = BaseValueForCurrentYear - BaseValueForStartingYear;

                if (expectedChange == 0)
                {
                    return actualChange >= 0 ? FrameworkGoalTrackingStatus.OnTrack : FrameworkGoalTrackingStatus.OffTrack;
                }

                var pace = actualChange / expectedChange;
                if (pace >= 0.95) return FrameworkGoalTrackingStatus.OnTrack;
                if (pace >= 0.7) return FrameworkGoalTrackingStatus.AtRisk;
                return FrameworkGoalTrackingStatus.OffTrack;
            }
        }

        /// <summary>
        /// Expected Target Value for Current Year = Base Value + (Annual Change Rate × Years Passed)
        /// Works correctly for both increase and decrease goals
        /// </summary>
        [NotMapped]
        public double ExpectedTargetValueForCurrentYear
        {
            get
            {
                var yearsPassed = CurrentYear - StartingYear;
                return BaseValueForStartingYear + (AnnualChangeRate * yearsPassed);
            }
        }

        /// <summary>
        /// Calculate the expected target value for any given year
        /// Works correctly for both increase and decrease goals
        /// </summary>
        /// <param name="year">The year to calculate the expected target for</param>
        /// <returns>The expected target value for the specified year</returns>
        public double GetExpectedTargetValueForYear(int year)
        {
            if (year < StartingYear || year > TargetYear)
                return 0;

            // StartingYear and TargetYear are always auto-calculated (anchors of the plan)
            if (year == StartingYear || year == TargetYear)
            {
                var yearsPassed = year - StartingYear;
                return BaseValueForStartingYear + (AnnualChangeRate * yearsPassed);
            }

            // In manual mode, stored manual targets are immutable — always use them
            // regardless of whether the year is currently the CurrentYear or not.
            if (ManualYearlyTargets && ManualExpectedTargets != null)
            {
                var manual = ManualExpectedTargets.FirstOrDefault(m => m.Year == year);
                if (manual != null)
                    return manual.ExpectedTargetValue;
            }

            var years = year - StartingYear;
            return BaseValueForStartingYear + (AnnualChangeRate * years);
        }

        /// <summary>
        /// Get the actual/base value for a specific year
        /// Returns the stored yearly value if it exists, otherwise returns 0
        /// </summary>
        /// <param name="year">The year to get the base value for</param>
        /// <returns>The base value for the specified year</returns>
        public double GetBaseValueForYear(int year)
        {
            if (year == StartingYear)
                return BaseValueForStartingYear;

            if (year == CurrentYear)
                return BaseValueForCurrentYear;

            if (year == TargetYear)
                return 0; // We don't store a base value for target year, only the target value

            // Check if there's a yearly value recorded for this year
            var yearlyValue = YearlyValues?.FirstOrDefault(yv => yv.Year == year);
            return yearlyValue?.ActualValue ?? 0;
        }
    }
}
