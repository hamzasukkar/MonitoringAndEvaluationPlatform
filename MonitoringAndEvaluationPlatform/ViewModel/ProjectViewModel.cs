using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MonitoringAndEvaluationPlatform.Models;

// NOTE: [Required] here previously resolved to Microsoft.Build.Framework.RequiredAttribute -
// an MSBuild task-parameter attribute that MVC model validation ignores entirely - because
// the file imported Microsoft.Build.Framework. It now uses the DataAnnotations attribute
// that was clearly intended.

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProjectViewModel
    {
        public Project project { get; set; } = new();

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
