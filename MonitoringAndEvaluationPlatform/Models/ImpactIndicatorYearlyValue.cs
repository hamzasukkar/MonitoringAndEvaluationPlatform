using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// One year's delivered value for an <see cref="ImpactIndicator"/>.
    ///
    /// Same shape as FrameworkGoalYearlyValue: surrogate int PK, parent FK, int Year, double value.
    /// "One value per year per indicator" is enforced by a unique index on
    /// (ImpactIndicatorId, Year) configured in ApplicationDbContext — not by a composite key,
    /// matching the convention used everywhere else in this schema.
    ///
    /// Values are cumulative increments: they sum to the indicator's achieved total.
    /// A year with no row means "not yet entered", which is distinct from a recorded zero.
    /// </summary>
    public class ImpactIndicatorYearlyValue
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int ImpactIndicatorId { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [Display(Name = "Value")]
        public double Value { get; set; }

        public DateTime DateRecorded { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("ImpactIndicatorId")]
        public ImpactIndicator ImpactIndicator { get; set; } = null!;
    }
}
