﻿using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class SubOutput
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public double IndicatorsPerformance { get; set; } = 0;
        public double DisbursementPerformance { get; set; } = 0;
        public double FieldMonitoring { get; set; } = 0;
        public double ImpactAssessment { get; set; } = 0;
        public double Weight { get; set; } = 1;
        public int OutputCode { get; set; }
        virtual public Output Output { get; set; }
        public ICollection<Indicator> Indicators { get; set; } = new List<Indicator>();
    }
}
