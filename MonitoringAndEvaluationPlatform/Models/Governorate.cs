using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Governorate
    {
        [Key]
        public string Code { get; set; }
        public string Name { get; set; }

        public ICollection<District> Districts { get; set; } = new List<District>();

        public ICollection<Project2> project2s { get; set; } = new List<Project2>();
    }
}
