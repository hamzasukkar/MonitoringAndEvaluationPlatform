using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ProgramFilterViewModel
    {
        public List<int> SelectedMinistries { get; set; } = new List<int>();
        public List<int> SelectedRegions { get; set; } = new List<int>();
        public List<int> SelectedDonors { get; set; } = new List<int>();
        public List<int> SelectedGoals { get; set; } = new List<int>();
        public List<int> SelectedSectors { get; set; } = new List<int>();
        public List<Ministry> Ministries { get; set; } = new List<Ministry>();
        public List<Donor> Donors { get; set; } = new List<Donor>();
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<Sector> Sectors { get; set; } = new List<Sector>();
        public List<Goal> Goals { get; set; } = new List<Goal>();

        public bool IsMinistryUser { get; set; } = false;
    }

}
