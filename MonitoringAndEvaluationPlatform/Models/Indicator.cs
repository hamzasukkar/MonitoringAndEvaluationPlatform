using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Indicator
    {
        [Key]
        public int IndicatorCode { get; set; }

        [Required]
        [Display(Name = "Indicator Name")]
        public string Name { get; set; }

        [Display(Name = "Source")]
        public string Source { get; set; } = string.Empty;

        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; } = string.Empty;

        [Display(Name = "Indicator Impact")]
        public string IndicatorImpact { get; set; } = "+"; // "+" or "-"

        [Display(Name = "Atif")]
        public string Atif { get; set; } = string.Empty;

        [Display(Name = "Indicators Performance")]
        public double IndicatorsPerformance { get; set; } = 0;

        [Display(Name = "Disbursement Performance")]
        public double DisbursementPerformance { get; set; } = 0;

        [Display(Name = "Field Monitoring")]
        public double FieldMonitoring { get; set; } = 0;

        [Display(Name = "Impact Assessment")]
        public double ImpactAssessment { get; set; } = 0;

        [Range(0.01, 1, ErrorMessage = "The Weight must be between 0 and 1 (exclusive of 0).")]
        [Display(Name = "Weight")]
        public double Weight { get; set; } = 1;

        public int SubOutputCode { get; set; }
        virtual public SubOutput SubOutput { get; set; }

        [Display(Name = "Is Common Indicator")]
        public bool IsCommon { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; }

        [Display(Name = "Target")]
        public int Target { get; set; } = 0;

        [DataType(DataType.Date)]
        [Display(Name = "Target Year")]
        public DateTime? TargetYear { get; set; }

        [Display(Name = "GAGRA")]
        public double GAGRA { get; set; } = 0;

        [Display(Name = "GAGRR")]
        public double GAGRR { get; set; } = 0;

        [Display(Name = "Trend")]
        public double Trend { get; set; } = 0;

        [Display(Name = "Concept")]
        public string Concept { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Method of Computation")]
        public string MethodOfComputation { get; set; } = string.Empty;

        [Display(Name = "Comment")]
        public string Comment { get; set; } = string.Empty;

        public ICollection<Measure> Measures { get; set; } = new List<Measure>();

        // Many-to-many with Project
        public ICollection<ProjectIndicator> ProjectIndicators { get; set; } = new List<ProjectIndicator>();

        /// <summary>
        /// Calculates and updates the Target based on the last provisional (forecast) measure
        /// </summary>
        public void CalculateTarget()
        {
            var lastProvisionalMeasure = Measures
                .Where(m => m.ValueType == MeasureValueType.Provisional)
                .OrderByDescending(m => m.Date)
                .FirstOrDefault();

            if (lastProvisionalMeasure != null)
            {
                Target = (int)lastProvisionalMeasure.Value;
                TargetYear = lastProvisionalMeasure.Date;
            }
        }

        /// <summary>
        /// Calculates GAGRA (Growth Average for Required Achievement)
        /// Formula: GAGRA = (Target - Baseline) / Number of years
        /// </summary>
        public void CalculateGAGRA()
        {
            var firstMeasure = Measures.OrderBy(m => m.Date).FirstOrDefault();
            if (firstMeasure != null && TargetYear.HasValue)
            {
                double baseline = firstMeasure.Value;
                int years = (TargetYear.Value.Year - firstMeasure.Date.Year);

                if (years > 0)
                {
                    GAGRA = Math.Round((Target - baseline) / years, 2);
                }
            }
        }

        /// <summary>
        /// Calculates GAGRR (Growth Average for Real Results)
        /// Formula: GAGRR = (Latest Real Value - Baseline) / Number of years elapsed
        /// </summary>
        public void CalculateGAGRR()
        {
            var realMeasures = Measures
                .Where(m => m.ValueType == MeasureValueType.Real)
                .OrderBy(m => m.Date)
                .ToList();

            if (realMeasures.Count >= 2)
            {
                double baseline = realMeasures.First().Value;
                var latestMeasure = realMeasures.Last();
                double latestValue = latestMeasure.Value;

                int years = (latestMeasure.Date.Year - realMeasures.First().Date.Year);

                if (years > 0)
                {
                    GAGRR = Math.Round((latestValue - baseline) / years, 2);
                }
            }
        }

        /// <summary>
        /// Calculates the Trend indicator
        /// Formula: Trend = (GAGRR / GAGRA) if GAGRA > 0, else 0
        /// </summary>
        public void CalculateTrend()
        {
            if (GAGRA > 0)
            {
                Trend = Math.Round(GAGRR / GAGRA, 2);
            }
            else
            {
                Trend = 0;
            }
        }

        /// <summary>
        /// Performs all automatic calculations for this indicator
        /// </summary>
        public void PerformAutomaticCalculations()
        {
            CalculateTarget();
            CalculateGAGRA();
            CalculateGAGRR();
            CalculateTrend();
        }
    }
}
