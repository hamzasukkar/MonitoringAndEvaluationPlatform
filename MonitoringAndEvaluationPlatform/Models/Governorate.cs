using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Governorate
    {
        [Key]
        public string Code { get; set; }
        public string EN_Name { get; set; }
        public string AR_Name { get; set; }

        public ICollection<District> Districts { get; set; } = new List<District>();
        public ICollection<Project> projects { get; set; } = new List<Project>();
    }
}
