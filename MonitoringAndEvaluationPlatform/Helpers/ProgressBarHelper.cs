namespace MonitoringAndEvaluationPlatform.Helpers
{
    public static class ProgressBarHelper
    {
        public static string GetProgressBarClass(double value)
        {
            return value switch
            {
                >= 80 => "bg-success",        // Green for excellent (80%+)
                >= 60 => "bg-warning",        // Yellow for good (60-79%)
                >= 40 => "progress-bar-orange", // Orange for fair (40-59%)
                >= 20 => "bg-danger",         // Red for poor (20-39%)
                _ => "bg-danger"              // Red for very poor (<20%)
            };
        }

        public static string GetTrendImage(double trend)
        {
            return trend switch
            {
                >= 80 => "external-link-success.png",
                >= 50 => "external-link-info.png",
                >= 30 => "external-link-squared.png",
                _ => "external-link-danger.png"
            };
        }
    }

}
