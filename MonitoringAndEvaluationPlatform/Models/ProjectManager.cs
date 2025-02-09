using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ProjectManager
    {
        [Key]
        public int Code { get; set; }

        public string Name { get; set; }
    }
}
