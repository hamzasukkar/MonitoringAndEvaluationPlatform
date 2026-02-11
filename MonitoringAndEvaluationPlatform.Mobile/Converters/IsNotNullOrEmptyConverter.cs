using System.Globalization;

namespace MonitoringAndEvaluationPlatform.Mobile.Converters
{
    public class IsNotNullOrEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s) return !string.IsNullOrEmpty(s);
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
