using Microsoft.AspNetCore.Mvc.Rendering;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProjectProgressFilterViewModel
    {
        public int? RegionId { get; set; }
        public int? SectorId { get; set; }
        public int? DonorId { get; set; }

        public List<SelectListItem> Regions { get; set; }
        public List<SelectListItem> Sectors { get; set; }
        public List<SelectListItem> Donors { get; set; }

        public List<ProjectProgressViewModel> Projects { get; set; }
    }

}
