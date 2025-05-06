using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Community
    {
        [Key]
        public int Code { get; set; }
        public string Name { get; set; }

        public int SubDistrictCode { get; set; }
        public SubDistrict SubDistrict { get; set; }
    }
}
