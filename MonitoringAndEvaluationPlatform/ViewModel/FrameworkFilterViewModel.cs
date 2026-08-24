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

        // Ministries shown per framework in the list, keyed by framework code: the owning
        // ministry plus any reached through its indicators' projects - the same union the
        // ministry filter and scoping use in FrameworksController.Index.
        public Dictionary<int, List<Ministry>> FrameworkMinistries { get; set; } = new Dictionary<int, List<Ministry>>();

        // Landing view: one row per ministry that owns at least one strategy.
        public List<MinistryStrategyGroup> MinistryGroups { get; set; } = new List<MinistryStrategyGroup>();

        // Strategies that belong to no ministry at all. They would be unreachable from the
        // ministries landing view, so it gets a row for them too.
        public int UnassignedStrategyCount { get; set; }

        // Roll-ups for the two bucket rows of the ministries table, so those rows carry the
        // same two metrics as the ministry rows instead of empty cells.
        public double UnassignedIndicatorsPerformance { get; set; }
        public double UnassignedDisbursementPerformance { get; set; }
        public double OwnerlessIndicatorsPerformance { get; set; }
        public double OwnerlessDisbursementPerformance { get; set; }

        // True on the landing view (ministries table), false once a ministry, the unassigned
        // bucket, or a search narrows the page down to a strategy list.
        public bool ShowMinistries { get; set; }
        public bool UnassignedOnly { get; set; }

        // Strategies with no OWNER (Framework.MinistryCode is null) even though a ministry may
        // still reach them through a project. UnassignedStrategyCount cannot surface these,
        // because it counts the owner+project union and those rows are not empty.
        public int OwnerlessStrategyCount { get; set; }
        public bool OwnerlessOnly { get; set; }

        public bool IsMinistryUser { get; set; } = false;
    }

    public class MinistryStrategyGroup
    {
        public Ministry Ministry { get; set; } = default!;
        public int StrategyCount { get; set; }

        // Averaged over the strategies counted in StrategyCount - NOT the stored
        // Ministry.IndicatorsPerformance / Ministry.DisbursementPerformance, which cover every
        // project of the ministry. Rolling up what the row links to keeps the number and the
        // strategy list you land on consistent.
        public double IndicatorsPerformance { get; set; }
        public double DisbursementPerformance { get; set; }
    }

}
