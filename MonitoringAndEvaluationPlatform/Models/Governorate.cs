using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Governorate
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }

        public ICollection<District> Districts { get; set; } = new List<District>();
    }
}
