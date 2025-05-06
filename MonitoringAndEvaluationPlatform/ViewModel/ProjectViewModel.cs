using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Framework;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProjectViewModel
    {
        public int ProjectID { get; set; }
      [Required]
        public string ProjectName { get; set; }
        public ICollection<Region> Regions { get; set; } = new List<Region>();
        public double EstimatedBudget { get; set; }
        public double RealBudget { get; set; }
        public int ProjectManagerCode { get; set; }
        public virtual ProjectManager ProjectManager { get; set; }
        public int SuperVisorCode { get; set; }
        public virtual SuperVisor SuperVisor { get; set; }
        public int MinistryCode { get; set; }
        public Ministry Ministry { get; set; }
        public int DonorCode { get; set; }
        public virtual Donor Donor { get; set; }
        public int SectorCode { get; set; }
        public virtual Sector Sector { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Required]
        public int GovernorateCode { get; set; }

        [Required]
        public int DistrictCode { get; set; }

        [Required]
        public int SubDistrictCode { get; set; }

        [Required]
        public int CommunityCode { get; set; }

        // For dropdowns
        public List<SelectListItem> Governorates { get; set; } = new();
        public List<SelectListItem> Districts { get; set; } = new();
        public List<SelectListItem> SubDistricts { get; set; } = new();
        public List<SelectListItem> Communities { get; set; } = new();
    }
}
