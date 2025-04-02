namespace MonitoringAndEvaluationPlatform.Helpers
{
    public static class ProgressBarHelper
    {
        public static string GetProgressBarClass(double value)
        {
            return value switch
            {
                > 80 => "progress-bar bg-success",
                > 60 => "rogress-bar bg-info",
                > 40 => "progress-bar-goldenYellow",
                > 20 => "progress-bar-orange",
                _ => "progress-bar bg-danger"
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
