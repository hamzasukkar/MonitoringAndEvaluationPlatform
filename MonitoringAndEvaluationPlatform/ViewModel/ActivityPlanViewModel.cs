namespace MonitoringAndEvaluationPlatform.ViewModel
{
    public class ActivityPlanViewModel
    {
        public string ActivityType { get; set; }
        public int PlanCount { get; set; }
        public List<ActivityRow> Activities { get; set; } = new List<ActivityRow>();
        public int PlansCount => Activities.Max(a => a.Dates.Count); // Get max plan count

    }

    public class ActivityRow
    {
        public string ActivityName { get; set; }
        public List<DateTime> Dates { get; set; } = new List<DateTime>();
        public List<int> PlannedValues { get; set; } = new List<int>();
        public List<int> RealisedValues { get; set; } = new List<int>();
        public int TotalEstimatedCost { get; set; }
        public int TotalRealisedCost { get; set; }
    }
}
