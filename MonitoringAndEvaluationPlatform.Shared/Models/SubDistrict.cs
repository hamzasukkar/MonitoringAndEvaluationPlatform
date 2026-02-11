using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class SubDistrict
    {
        [Key]
        public string Code { get; set; }
        public string EN_Name { get; set; }
        public string AR_Name { get; set; }

        public string DistrictCode { get; set; }
        public District District { get; set; }

        public ICollection<Community> Communities { get; set; } = new List<Community>();
        public ICollection<Project> projects { get; set; } = new List<Project>();
    }
}
