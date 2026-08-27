using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// One dated achievement recorded against an <see cref="ImpactIndicator"/> — "15 schools in May".
    ///
    /// Not to be confused with <see cref="Measure"/>, which is a percentage-point of progress on a
    /// ProjectPhase capped so the phase total stays at 100. Here <see cref="Value"/> is an absolute
    /// amount in the indicator's unit, and the running total may exceed the target.
    /// </summary>
    public class ImpactAchievement
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Achievement date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Achievement value is required.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Achievement value must be greater than zero.")]
        [Display(Name = "Value")]
        public double Value { get; set; }

        [StringLength(1000)]
        [Display(Name = "Note")]
        public string? Note { get; set; }

        public int ImpactIndicatorId { get; set; }
        public virtual ImpactIndicator ImpactIndicator { get; set; } = null!;
    }
}
