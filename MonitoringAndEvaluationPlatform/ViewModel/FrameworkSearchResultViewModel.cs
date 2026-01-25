using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class FrameworkSearchResultViewModel
    {
        public Framework Framework { get; set; }
        public List<SearchMatch> Matches { get; set; } = new List<SearchMatch>();
        public int TotalMatches => Matches.Count;
    }

    public class SearchMatch
    {
        public string Type { get; set; } // "Framework", "Outcome", "Output", "SubOutput", "Indicator", "Project"
        public string Name { get; set; }
        public string NavigationUrl { get; set; }
        public string Icon { get; set; }
        public string ParentPath { get; set; } // Breadcrumb path
        public int? Code { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
