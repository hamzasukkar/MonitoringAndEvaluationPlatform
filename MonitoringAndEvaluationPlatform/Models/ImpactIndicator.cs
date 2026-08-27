using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MonitoringAndEvaluationPlatform.Attributes;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A quantitative impact indicator owned by a single project — "120 schools rehabilitated".
    ///
    /// This is deliberately NOT the same thing as <see cref="Indicator"/>, which belongs to the
    /// results framework (SubOutput → Output → Outcome → Framework) and whose IndicatorsPerformance
    /// is just a copy of the project's execution performance.
    ///
    /// Impact indicators are a parallel track: nothing here feeds project.performance or the
    /// framework roll-up. They measure what the project produced, not how far along it is.
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
        /// achieved numbers are meaningless in reports.
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Unit")]
        public string? Unit { get; set; }

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Target value is required.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Target value must be greater than zero.")]
        [Display(Name = "Target Value")]
        public double TargetValue { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [DateRangeValidation(nameof(StartDate))]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Cumulative achieved value — the sum of every achievement's value.
        /// Persisted so listings and reports can sort and filter without loading achievements,
        /// and written by exactly one place: <c>ImpactIndicatorService.RecalculateAsync</c>.
        /// </summary>
        [Display(Name = "Achieved Value")]
        public double AchievedValue { get; set; } = 0;

        /// <summary>
        /// Achievement as a percentage of the total target. Computed, never stored, so it can
        /// never drift out of sync with <see cref="AchievedValue"/>.
        /// Deliberately uncapped: exceeding the target shows as more than 100%.
        /// </summary>
        [NotMapped]
        [Display(Name = "Achievement Rate")]
        public double AchievementRate =>
            TargetValue > 0 ? AchievedValue / TargetValue * 100 : 0;

        /// <summary>Remaining amount to reach the target; zero once the target is met or exceeded.</summary>
        [NotMapped]
        public double RemainingToTarget => Math.Max(TargetValue - AchievedValue, 0);

        public int ProjectID { get; set; }
        public virtual Project Project { get; set; } = null!;

        public virtual ICollection<ImpactAchievement> Achievements { get; set; } = new List<ImpactAchievement>();

        /// <summary>
        /// Framework-level impact targets this indicator contributes to. Delete is restricted
        /// while any link exists, so removing a linked indicator fails loudly.
        /// </summary>
        public virtual ICollection<FrameworkImpactIndicator> FrameworkLinks { get; set; }
            = new List<FrameworkImpactIndicator>();
    }
}
