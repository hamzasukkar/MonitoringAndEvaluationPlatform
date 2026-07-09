using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class PublicSectorType
    {
        [Key]
        public int Code { get; set; }
        public string EN_Name { get; set; }
        public string AR_Name { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
