using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// Links one <see cref="ImpactIndicator"/> to one <see cref="FrameworkImpact"/> with a weight.
    ///
    /// Note what is NOT here: the indicator's name and value. Those are read live from
    /// <see cref="ImpactIndicator"/> at display time, so recording an achievement on the project
    /// page immediately moves the framework's weighted rate with no recalculation step.
    /// The only thing this row owns is the weight.
    /// </summary>
    public class FrameworkImpactIndicator
    {
        [Key]
        public int Id { get; set; }

        public int FrameworkImpactId { get; set; }
        public virtual FrameworkImpact FrameworkImpact { get; set; } = null!;

        public int ImpactIndicatorId { get; set; }
        public virtual ImpactIndicator ImpactIndicator { get; set; } = null!;

        /// <summary>
        /// Relative weight on a 0-100 scale; the weights of one FrameworkImpact must sum to 100.
        /// (0-100 is the live convention across this codebase — see Helpers/PercentHelper.cs —
        /// even though the older Indicator.Weight still carries a stale [Range(0,1)] attribute.)
        /// </summary>
        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100.")]
        [Display(Name = "Weight (%)")]
        public double Weight { get; set; }
    }
}
