using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class District
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }

        public int GovernorateCode { get; set; }
        public Governorate Governorate { get; set; }

        public ICollection<SubDistrict> SubDistricts { get; set; } = new List<SubDistrict>();
    }
}
