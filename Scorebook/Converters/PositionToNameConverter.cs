using Electrons.Core.Net8.Games;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Scorebook.Converters
{
    internal class PositionToNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var lineup = values[0] as ObservableCollection<Player>;
            var targetPos = values[1] as string; // e.g., "1B"

            if (lineup == null || string.IsNullOrEmpty(targetPos)) return "";

            // Find the player assigned to this spot
            var player = lineup.FirstOrDefault(p => p.Position?.PositionString == targetPos);
            return player?.LastName ?? "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
