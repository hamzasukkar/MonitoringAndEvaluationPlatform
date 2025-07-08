namespace MonitoringAndEvaluationPlatform.ViewModel
{
    // New class to hold all data for a single plan/column
    public class PlanDetail
    {
        public int PlanCode { get; set; } // The unique ID we need!
        public DateTime Date { get; set; }
        public int PlannedValue { get; set; }
        public int RealisedValue { get; set; }
    }

    public class ActivityRow
    {
        // We no longer need the separate lists for Dates, PlannedValues, etc.
        public string ActivityName { get; set; }

        // This is the key change: A single list of PlanDetail objects
        public List<PlanDetail> Plans { get; set; } = new List<PlanDetail>();

        // The totals can now be calculated easily from the new Plans list
        public int TotalEstimatedCost => Plans.Sum(p => p.PlannedValue);
        public int TotalRealisedCost => Plans.Sum(p => p.RealisedValue);
    }

    public class ActivityPlanViewModel
    {
        public string ActivityType { get; set; }
        public List<ActivityRow> Activities { get; set; } = new List<ActivityRow>();

        // This calculates the maximum number of columns needed for the table header
        public int PlansCount => Activities.Any() ? Activities.Max(a => a.Plans.Count) : 0;
    }
}