using CommunityToolkit.Mvvm.ComponentModel;
using Electrons.Core.Net8.Games;
using System.Collections.ObjectModel;

namespace Scorebook.ViewObjects
{
    public class DefensiveAlignment : ObservableObject
    {        
        private readonly Dictionary<string, string> _defense = [];        

        internal void RefreshPositions(ObservableCollection<LineupPosition> lineup)
        {
            _defense.Clear();
            if (lineup != null)
            {
                foreach (var player in lineup)
                    if (player.Position != null)
                        _defense[player.Position.PositionString] = player.Player.LastName;

                foreach (var player in lineup.Where(w => w.HittingFor is not null).Select(s => s.HittingFor))
                    if (player.Position is not null)
                        _defense[player.Position.PositionString] = player.LastName;
            }

        }
        public string this[string pos]=>_defense.GetValueOrDefault(pos,"---");
    }
}
