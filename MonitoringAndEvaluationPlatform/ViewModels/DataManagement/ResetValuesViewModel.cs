namespace MonitoringAndEvaluationPlatform.ViewModels.DataManagement
{
    public class ResetValuesViewModel
    {
        public bool ResetPlans { get; set; }
        public bool ResetMeasures { get; set; }
        public bool ResetProjectPerformance { get; set; }
        public bool RecalculateHierarchy { get; set; }

        // Statistics
        public int TotalPlans { get; set; }
        public int TotalMeasures { get; set; }
        public int TotalProjects { get; set; }
    }
}
