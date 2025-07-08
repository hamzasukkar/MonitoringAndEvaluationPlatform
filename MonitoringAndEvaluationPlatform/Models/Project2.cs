using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Project2
    {
        [Key]
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }

        public ICollection<Governorate> Governorates { get; set; } = new List<Governorate>();
        public ICollection<District> Districts { get; set; } = new List<District>();
        public ICollection<SubDistrict> SubDistricts { get; set; } = new List<SubDistrict>();
        public ICollection<Community> Communities { get; set; } = new List<Community>();

    }
}
