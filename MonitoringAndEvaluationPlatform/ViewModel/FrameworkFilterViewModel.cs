using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class FrameworkFilterViewModel
    {
        public List<int> SelectedMinistries { get; set; } = new List<int>();
        public List<int> SelectedSector { get; set; } = new List<int>();
        public List<int> SelectedDonors { get; set; } = new List<int>();
        public List<Ministry> Ministries { get; set; } = new List<Ministry>();
        public List<Donor> Donors { get; set; } = new List<Donor>();
        public List<Framework> Frameworks { get; set; } = new List<Framework>();
        public List<Sector> Sectors { get; set; } = new List<Sector>();

        public bool IsMinistryUser { get; set; } = false;
    }

}
