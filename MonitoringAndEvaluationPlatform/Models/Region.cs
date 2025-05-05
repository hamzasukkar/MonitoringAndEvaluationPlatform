using MonitoringAndEvaluationPlatform.Enums;
using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Region
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        // Use the RegionType enum
        [Display(Name = "Region Type")]
        public RegionType Type { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();

    }
}
