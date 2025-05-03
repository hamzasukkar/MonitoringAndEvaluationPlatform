using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProgramFilterViewModel
    {
        public List<int> SelectedMinistries { get; set; } = new List<int>();
        public List<int> SelectedRegions { get; set; } = new List<int>();
        public List<int> SelectedDonors { get; set; } = new List<int>();
        public List<Ministry> Ministries { get; set; } = new List<Ministry>();
        public List<Region> Regions { get; set; } = new List<Region>();
        public List<Donor> Donors { get; set; } = new List<Donor>();
        public List<Project> Projects { get; set; } = new List<Models.Project>();

        public bool IsMinistryUser { get; set; } = false;
    }

}
