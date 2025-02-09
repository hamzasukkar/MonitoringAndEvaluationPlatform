using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProgramFilterViewModel
    {
        public List<int> SelectedMinistries { get; set; } = new List<int>();
        public List<int> SelectedRegions { get; set; } = new List<int>();
        public List<int> SelectedDonors { get; set; } = new List<int>();
        public List<Ministrie> Ministries { get; set; } = new List<Ministrie>();
        public List<Region> Regions { get; set; } = new List<Region>();
        public List<Donor> Donors { get; set; } = new List<Donor>();
        public List<Models.Program> Programs { get; set; } = new List<Models.Program>();
    }

}
