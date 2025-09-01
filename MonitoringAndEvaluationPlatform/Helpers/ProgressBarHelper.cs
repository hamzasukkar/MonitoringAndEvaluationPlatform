namespace MonitoringAndEvaluationPlatform.Helpers
{
    public static class ProgressBarHelper
    {
        public static string GetProgressBarClass(double value)
        {
            return value switch
            {
                > 80 => "bg-success",
                > 60 => "progress-bar-yellow",
                > 40 => "bg-warning",
                > 20 => "progress-bar-orange",
                _ => "bg-danger"
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
