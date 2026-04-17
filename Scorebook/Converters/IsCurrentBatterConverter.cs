using Electrons.Core.Net8.Games;
using System.Globalization;

namespace Scorebook.Converters
{
    public class IsCurrentBatterConverter : IMultiValueConverter
    {       

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] -> CurrentAb (from ViewModel)
            // values[1] -> Player (from the Row)

            if (values == null || values.Length < 2) return false;

            var currentAb = values[0] as AtBat; // Replace with your actual class
            var rowPlayer = values[1] as Player; // Replace with your actual class

            if (currentAb?.Batter is null || rowPlayer is null)
                return false;

            return currentAb.Batter.FullName == rowPlayer.FullName;
        }

        

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
