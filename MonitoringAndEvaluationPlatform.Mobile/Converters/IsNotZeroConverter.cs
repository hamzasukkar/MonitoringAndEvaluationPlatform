using System.Globalization;

namespace MonitoringAndEvaluationPlatform.Mobile.Converters
{
    public class IsNotZeroConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int i) return i != 0;
            if (value is double d) return d != 0;
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
