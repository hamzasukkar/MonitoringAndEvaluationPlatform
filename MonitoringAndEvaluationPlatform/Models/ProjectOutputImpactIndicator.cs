using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// Join entity for ProjectOutput &lt;-&gt; ImpactIndicator, carrying the Weight (as a
    /// percentage, 0-100) the user assigns to each indicator when linking it to a project output.
    ///
    /// Follows the same shape as ProjectDonor (a plain skip-navigation promoted to an explicit
    /// join entity once it needs a payload): surrogate int PK, not composite.
    ///
    /// Weights for every indicator linked to the SAME project output must sum to exactly 100 —
    /// the same convention and tolerance (0.01) as ProjectPhase.Weight, enforced in
    /// ImpactController.Create rather than here (validating across sibling rows needs the whole
    /// set, which a single entity's own attributes cannot see). The consuming formulas,
    /// ProjectOutput.WeightedAchievementRate and GetWeightedPercentageForYear, already normalize
    /// by the total weight, so they did not need to change for this constraint — only the input
    /// validation and the Create form's UI did.
    /// </summary>
    public class ProjectOutputImpactIndicator
    {
        [Key]
        public int Id { get; set; }

        public int ProjectOutputId { get; set; }
        public ProjectOutput ProjectOutput { get; set; } = null!;

        public int ImpactIndicatorId { get; set; }
        public ImpactIndicator ImpactIndicator { get; set; } = null!;

        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100.")]
        [Display(Name = "Weight (%)")]
        public double Weight { get; set; } = 100;
    }
}
