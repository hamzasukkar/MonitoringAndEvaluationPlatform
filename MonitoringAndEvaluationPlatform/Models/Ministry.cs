﻿using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Ministry

    {
        [Key]
        public int Code { get; set; }
        public string MinistryName { get; set; }
        public int DisbursementPerformance { get; set; }
        public int FieldMonitoring { get; set; }
        public int ImpactAssessment { get; set; }
    }
}
