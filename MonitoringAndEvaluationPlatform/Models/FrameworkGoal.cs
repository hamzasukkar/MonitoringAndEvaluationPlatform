using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Calculated Properties (Not Mapped to Database)

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
        /// Progress Rate = (Current Value - Base Value) / (Target Value - Base Value)
        /// This measures the progress from base value to current year as a proportion of the total goal
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

                return numerator / denominator;
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

            var yearsPassed = year - StartingYear;
            return BaseValueForStartingYear + (AnnualChangeRate * yearsPassed);
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
