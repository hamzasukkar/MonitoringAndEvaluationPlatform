﻿using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Outcome
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public double Trend { get; set; } = 0;
        public int IndicatorsPerformance { get; set; } = 0;
        public int DisbursementPerformance { get; set; } = 0;
        public int FieldMonitoring { get; set; } = 0;
        public int ImpactAssessment { get; set; } = 0;

        // Foreign key and navigation property for Framework
        public int FrameworkCode { get; set; }
        virtual public Framework Framework { get; set; }
        public ICollection<Output> Outputs { get; set; } = new List<Output>();
    }
}
