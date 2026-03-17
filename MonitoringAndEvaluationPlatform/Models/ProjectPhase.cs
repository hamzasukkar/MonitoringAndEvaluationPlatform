using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ProjectPhase
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Phase name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Phase name must be between 2 and 200 characters.")]
        [Display(Name = "Phase Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required.")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Budget is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Budget must be a positive value.")]
        [Display(Name = "Budget")]
        public double Budget { get; set; }

        /// <summary>
        /// Weight as a percentage (0-100). All phases for a project must sum to exactly 100.
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Weight (%)")]
        public decimal Weight { get; set; } = 100;

        /// <summary>
        /// Calculated performance (0-100) based on the average of all Measure values in this phase.
        /// </summary>
        [Display(Name = "Phase Performance (%)")]
        public double PhasePerformance { get; set; } = 0;

        /// <summary>
        /// The total quantity target for this phase (e.g. 100 trees, 50 km).
        /// When set, measure Value is auto-calculated as (Quantity / TargetQuantity) × 100.
        /// </summary>
        [Display(Name = "Target Quantity")]
        [Range(0, double.MaxValue, ErrorMessage = "Target quantity must be a positive value.")]
        public double? TargetQuantity { get; set; }

        // Foreign key to Project
        public int ProjectID { get; set; }
        public virtual Project Project { get; set; } = null!;

        // Measures belong to this phase
        public virtual ICollection<Measure> Measures { get; set; } = new List<Measure>();

        // Action Plan belongs to this phase (1-to-1)
        public virtual ActionPlan? ActionPlan { get; set; }
    }
}
