﻿using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Community
    {
        [Key]
        public string Code { get; set; }
        public string Name { get; set; }

        public string SubDistrictCode { get; set; }
        public SubDistrict SubDistrict { get; set; }

        public ICollection<Project> projects { get; set; } = new List<Project>();
    }
}
