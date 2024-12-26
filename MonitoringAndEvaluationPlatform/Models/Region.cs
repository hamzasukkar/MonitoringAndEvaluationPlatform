using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Region
    {
        [Key]
        public int Code { get; set; }
        public string Nom { get; set; }
        public string Type { get; set; }
    }
}
