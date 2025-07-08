namespace MonitoringAndEvaluationPlatform.Helpers
{
    public class ProgressHelper
    {
        public static string GetProgressClass(double value)
        {
            if (value > 80) return "progress-success value-success";
            if (value > 60) return "progress-info value-info";
            if (value > 40) return "progress-warning value-warning";
            if (value > 20) return "progress-orange value-orange";
            return "progress-danger value-danger";
        }

        public static (string BarClass, string ValueClass) GetProgressClasses(double value)
        {
            if (value > 80) return ("progress-success", "value-success");
            if (value > 60) return ("progress-info", "value-info");
            if (value > 40) return ("progress-warning", "value-warning");
            if (value > 20) return ("progress-orange", "value-orange");
            return ("progress-danger", "value-danger");
        }
    }
}
