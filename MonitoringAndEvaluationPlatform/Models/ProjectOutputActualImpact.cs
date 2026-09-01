using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// One year's hand-recorded actual impact for a <see cref="ProjectOutput"/>.
    ///
    /// Same shape as ImpactIndicatorYearlyValue and FrameworkGoalYearlyValue: surrogate int PK,
    /// parent FK, int Year, double value. "One value per year per output" is enforced by a unique
    /// index on (ProjectOutputId, Year) configured in ApplicationDbContext — not by a composite
    /// key, matching the convention used everywhere else in this schema. That index is also what
    /// makes the controller's find-or-add save safe against a double submit.
    ///
    /// Deliberately INDEPENDENT of the computed figures on the Impact table: Amount of Change and
    /// Total Value are derived from the linked indicators, while this is whatever the user
    /// observed. Comparing the two is the point — nothing here feeds back into the calculations.
    ///
    /// A year with no row means "not yet recorded", which is distinct from a recorded zero.
    /// </summary>
    public class ProjectOutputActualImpact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectOutputId { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [Display(Name = "Actual Impact")]
        public double Value { get; set; }

        public DateTime DateRecorded { get; set; } = DateTime.Now;

        [ForeignKey("ProjectOutputId")]
        public ProjectOutput ProjectOutput { get; set; } = null!;
    }
}
