﻿using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Enums;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Indicator
    {
        [Key]
        public int IndicatorCode { get; set; }
        public string Name { get; set; }
        public string Source { get; set; } = string.Empty;
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;

        [Range(0, 1, ErrorMessage = "The Weight must be between 0 and 1.")]
        public double Weight { get; set; } = 1;
        public int SubOutputCode { get; set; }
        virtual public SubOutput SubOutput { get; set; }
        public bool IsCommon { get; set; }
        public bool Active { get; set; }
        public int Target { get; set; } = 0;

        [DataType(DataType.Date)]
        public DateTime TargetYear { get; set; }
        public double GAGRA { get; set; } = 0;
        public double GAGRR { get; set; } = 0;
        public string Concept { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MethodOfComputation { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public ICollection<Measure> Measures { get; set; } = new List<Measure>();

        // New: Many-to-many with Project
        public ICollection<ProjectIndicator> ProjectIndicators { get; set; } = new List<ProjectIndicator>();

        //virtual public Program Program { get; set; }

    }
}
