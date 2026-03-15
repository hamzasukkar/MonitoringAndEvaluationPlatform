using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class FrameworkGoalManualExpectedTarget
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int FrameworkGoalID { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public double ExpectedTargetValue { get; set; }

        // Navigation property
        [ForeignKey("FrameworkGoalID")]
        public FrameworkGoal FrameworkGoal { get; set; }
    }
}