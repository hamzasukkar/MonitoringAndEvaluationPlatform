using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class District
    {
        [Key]
        public string Code { get; set; }
        public string EN_Name { get; set; }
        public string AR_Name { get; set; }

        public string GovernorateCode { get; set; }
        public Governorate Governorate { get; set; }

        public ICollection<SubDistrict> SubDistricts { get; set; } = new List<SubDistrict>();

        public ICollection<Project> projects { get; set; } = new List<Project>();
    }
}
