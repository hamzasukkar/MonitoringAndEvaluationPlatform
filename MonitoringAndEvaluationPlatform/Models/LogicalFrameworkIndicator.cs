﻿using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class LogicalFrameworkIndicator
    {
        [Key]
        public int IndicatorCode { get; set; }
        public string Name { get; set; }
        public double Performance { get; set; } = 0;

        [Range(0, 1, ErrorMessage = "The Weight must be between 0 and 1.")]
        public double Weight { get; set; } = 1;
        public int LogicalFrameworkCode { get; set; }
        virtual public LogicalFramework LogicalFramework { get; set; }
        public bool IsCommon { get; set; }
        public bool Active { get; set; } = true;
        public int Target { get; set; } = 0;

        [DataType(DataType.Date)]
        public DateTime TargetYear { get; set; }
        public double GAGRA { get; set; } = 0;
        public double GAGRR { get; set; } = 0;
        public string Concept { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ICollection<LogicalMeasure> logicalMeasures { get; set; } = new List<LogicalMeasure>();
    }
}
