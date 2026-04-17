using System.Globalization;

namespace Scorebook.Converters
{
    internal class IntToColorConverter : IValueConverter
    {
        public Color ActiveColor { get; set; } = Color.FromArgb("#800000");
        public Color InActiveColor { get; set; } = Colors.LightGrey;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolVal && boolVal)
                return ActiveColor;
            if (value is int currentCount && parameter is string thresholdString)
            {
                int threshold = int.Parse(thresholdString);

                // If the current count is greater than or equal to this pip's number, light it up!
                return currentCount >= threshold ? ActiveColor : InActiveColor;
            }
            return InActiveColor;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
