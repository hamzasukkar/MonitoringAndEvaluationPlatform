using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class CreateProjectViewModel
    {
        public string ProjectName { get; set; }
        public int RegionCode { get; set; }
        public int ProjectManagerCode { get; set; }
        public int SuperVisorCode { get; set; }
        public int MinistryCode { get; set; }
        public int DonorCode { get; set; }
        public double EstimatedBudget { get; set; }
        public double RealBudget { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<int> SelectedFrameworkIds { get; set; } = new List<int>();

        public List<Framework> AvailableFrameworks { get; set; } = new List<Framework>();
    }
}
