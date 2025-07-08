using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Framework;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProjectViewModel
    {
        public Project project { get; set; }

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
