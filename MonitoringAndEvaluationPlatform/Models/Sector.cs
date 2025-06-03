using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Sector
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();

    }
}
