using CommunityToolkit.Mvvm.ComponentModel;
using Electrons.Core.Net8.Games;

namespace Scorebook.ViewObjects
{
    public class DefensiveAlignment : ObservableObject
    {
        private Team? _fieldingTeam;
        private readonly Dictionary<string, string> _defense = [];
        public Team? FieldingTeam
        {
            get { return _fieldingTeam; }
            set
            {
                if (SetProperty(ref _fieldingTeam, value))
                {
                    RefreshPositions();
                }
            }
        }

        private void RefreshPositions()
        {
            _defense.Clear();
            if (_fieldingTeam?.Lineup != null)
            {
                foreach (var player in _fieldingTeam.Lineup)
                    if (player.Position != null)
                        _defense[player.Position.PositionString] = player.LastName;

                foreach (var player in _fieldingTeam.Lineup.Where(w => w.HittingFor is not null).Select(s => s.HittingFor))
                    if (player.Position is not null)
                        _defense[player.Position.PositionString] = player.LastName;
            }

        }
        public string this[string pos]=>_defense.GetValueOrDefault(pos,"---");
    }
}
