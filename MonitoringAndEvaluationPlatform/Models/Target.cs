﻿using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Target
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public double Trend { get; set; }
        public int IndicatorsPerformance { get; set; }
        public int DisbursementPerformance { get; set; }
        public int FieldMonitoring { get; set; }
        public int ImpactAssessment { get; set; }
    }
}
