using System.Globalization;

namespace MonitoringAndEvaluationPlatform.Mobile.Converters
{
    public class PerformanceToProgressConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double performance)
                return Math.Clamp(performance / 100.0, 0.0, 1.0);
            return 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
