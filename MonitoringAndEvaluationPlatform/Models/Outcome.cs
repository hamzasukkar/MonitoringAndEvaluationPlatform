﻿using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Outcome
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public double Trend { get; set; } = 0;
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;

        [Range(0, 1, ErrorMessage = "The Weight must be between 0 and 1.")]
        public double Weight { get; set; } = 1;

        // Foreign key and navigation property for Framework
        public int FrameworkCode { get; set; }
        virtual public Framework Framework { get; set; }
        public ICollection<Output> Outputs { get; set; } = new List<Output>();
    }
}
