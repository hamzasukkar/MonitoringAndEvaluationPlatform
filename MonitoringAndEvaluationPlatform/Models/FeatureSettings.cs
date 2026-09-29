namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// UI feature toggles, bound from the "Features" section of appsettings.json.
    /// </summary>
    public class FeatureSettings
    {
        /// <summary>
        /// Shows supervisors across the UI (Set Up nav, project tables, reports, user guide).
        /// Hidden by default; supervisor data stays in the database because
        /// Project.SuperVisorCode is a required foreign key. Set to true to bring it back.
        /// </summary>
        public bool ShowSupervisors { get; set; } = false;
    }
}
