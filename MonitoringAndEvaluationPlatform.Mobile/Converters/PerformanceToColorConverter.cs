using System.Globalization;

namespace MonitoringAndEvaluationPlatform.Mobile.Converters
{
    public class PerformanceToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double performance)
            {
                return performance switch
                {
                    >= 75 => Colors.Green,
                    >= 50 => Colors.Orange,
                    _ => Colors.Red
                };
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
