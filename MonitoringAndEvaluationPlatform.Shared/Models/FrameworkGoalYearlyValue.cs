using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class FrameworkGoalYearlyValue
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int FrameworkGoalID { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public double ActualValue { get; set; }

        public DateTime DateRecorded { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("FrameworkGoalID")]
        public FrameworkGoal FrameworkGoal { get; set; }
    }
}
