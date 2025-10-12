using MonitoringAndEvaluationPlatform.Models;
using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProjectWithMeasuresViewModel
    {
        public Project Project { get; set; } = new Project();
        public List<MeasureInputModel> TargetMeasures { get; set; } = new List<MeasureInputModel>();
        public List<Indicator> AvailableIndicators { get; set; } = new List<Indicator>();
    }

    public class MeasureInputModel
    {
        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Value must be greater than 0")]
        public double Value { get; set; }

        [Required]
        public int IndicatorCode { get; set; }

        public string IndicatorName { get; set; } = string.Empty;
    }
}