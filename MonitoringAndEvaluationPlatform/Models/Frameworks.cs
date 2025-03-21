﻿using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Framework
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public double Trend { get; set; } = 0;
        public int IndicatorsPerformance { get; set; } = 0;
        public int DisbursementPerformance { get; set; } = 0;
        public int FieldMonitoring { get; set; } = 0;
        public int ImpactAssessment { get; set; } = 0;

        [Range(0, 1, ErrorMessage = "The Weight must be between 0 and 1.")]
        public double Weight { get; set; } = 1;

        // Navigation property for related Outcomes
        public ICollection<Outcome> Outcomes { get; set; } = new List<Outcome>();
    }
}
