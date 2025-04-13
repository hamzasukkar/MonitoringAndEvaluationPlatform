using Microsoft.AspNetCore.Mvc.Rendering;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProjectProgress2ViewModel
    {
        public int? RegionId { get; set; }
        public int? SectorId { get; set; }
        public int? DonorId { get; set; }

        public List<SelectListItem> Regions { get; set; }
        public List<SelectListItem> Sectors { get; set; }
        public List<SelectListItem> Donors { get; set; }

        public List<ProjectProgressItem> Projects { get; set; } = new();
    }
    public class ProjectProgressItem
    {
        public string ProjectName { get; set; }
        public double CompletionRate { get; set; } // value between 0–100
        public int TotalIndicators { get; set; }
        public double TotalTarget { get; set; }
        public double TotalAchieved { get; set; }
    }
}
