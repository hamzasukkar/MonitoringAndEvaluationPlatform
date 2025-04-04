using Microsoft.AspNetCore.Mvc.Rendering;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProgramViewModel
    {
        public Models.Project Program { get; set; }

        // Dropdown or selection for regions
        public List<SelectListItem> Regions { get; set; } = new List<SelectListItem>();

    }


}
