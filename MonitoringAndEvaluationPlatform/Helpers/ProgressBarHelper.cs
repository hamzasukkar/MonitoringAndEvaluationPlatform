namespace MonitoringAndEvaluationPlatform.Helpers
{
    public static class ProgressBarHelper
    {
        public static string GetProgressBarClass(int value)
        {
            return value switch
            {
                > 80 => "progress-bar-success",
                > 60 => "progress-bar-yellow",
                > 40 => "progress-bar-goldenYellow",
                > 20 => "progress-bar-orange",
                _ => "progress-bar-danger"
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
