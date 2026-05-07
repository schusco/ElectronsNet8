using CommunityToolkit.Mvvm.Input;
using Electrons.Core.Net8.Games;
using Scorebook.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Scorebook.ViewObjects
{
    public class TeamWrapper : INotifyPropertyChanged
    {
        public TeamWrapper(ScorebookViewModel vm, Team coreTeam, bool isHome, bool unknown)
        {
            _vm = vm;
            CoreTeam = coreTeam;
            _isHome = isHome;
            _isUnknownRoster = unknown;
            IsEditing = false;
            foreach (var player in CoreTeam.Roster)
                Bench.Add(player);
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ScorebookViewModel.MobileActionTrigger))
                {
                    OnPropertyChanged(nameof(MobileText));
                }
            };
        }

        public Team CoreTeam { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public string TeamName => $"{CoreTeam?.Name} ({(_isHome ? "Home" : "Away")})";
        public string MobileText
        {
            get => _vm.MobileActionTrigger ? _mobileText : "";
            set
            {
                _mobileText = value;
                OnPropertyChanged(nameof(MobileText));
            }
        }
        public ObservableCollection<Player> TeamPlayers { get; set; } = [];
        public ObservableCollection<PositionStatus> PositionStatusList { get; set; } = [];
        public ObservableCollection<LineupPosition> Lineup { get; set; } = [];
        public ObservableCollection<Player> Bench { get; set; } = [];
        public ObservableCollection<Player> Replaced { get; set; } = [];
        public Dictionary<Position, bool> PositionOccupiedMap { get; set; } = [];
        public DefensiveAlignment Defense { get; set; } = new DefensiveAlignment();
        public bool PitcherSelected
        {
            get => _pitcherSelected;
            set
            {
                _pitcherSelected = value;
                OnPropertyChanged(nameof(PitcherSelected));
                OnPropertyChanged(nameof(PitcherText));
                OnPropertyChanged(nameof(PitcherName));
            }
        }
        public string PitcherText => _pitcherSelected ? "Change Pitcher" : "Set Pitcher";
        public string SubText => CoreTeam?.OrderIsSet ?? false ? "View Lineup" : "Set Lineup";
        public string PitcherName => CoreTeam?.CurrentPitcher?.FullName ?? "Not Set";
        public bool IsUnknownRoster
        {
            get => _isUnknownRoster;
            set
            {
                _isUnknownRoster = value;
                OnPropertyChanged(nameof(IsUnknownRoster));
            }
        }
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
                if (_isHome)
                    _vm.EditingHomeLineup = value;
                else
                    _vm.EditingAwayLineup = value;
            }
        }
        internal void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        internal void UpdateLineup(LineupPosition lp, Player newPlayer)
        {
            int index = Lineup.IndexOf(lp);
            Lineup.RemoveAt(index);
            Lineup.Insert(index, new LineupPosition(newPlayer, index + 1));
        }
        internal void FillLineup()
        {
            var lp = 1;
            foreach (var player in CoreTeam.Lineup)
                Lineup.Add(new LineupPosition(player, lp++));
            _vm.RosterCoordinator.UpdatePitcherUI(this);
        }
        internal void UpdatePositionAvailability()
        {
            foreach (var pos in Position.All)
                PositionOccupiedMap[pos] = false;
            foreach (var lp in Lineup)
            {
                lp.IsConflict = lp.Position == Position.EH ? false : PositionOccupiedMap[lp.Position];
                PositionOccupiedMap[lp.Position] = true;
                if (lp.HasDH)
                {
                    lp.IsConflict = lp.HittingFor.Position == Position.EH ? false : PositionOccupiedMap[lp.HittingFor.Position];
                    PositionOccupiedMap[lp.HittingFor.Position] = true;
                }
            }
            var counts = Lineup.Where(p => p.Position != null).GroupBy(p => p.Position).ToDictionary(g => g.Key, g => g.Count());
            var dhPositions = Lineup.Select(s => s.HittingFor).Where(w => w is not null).Select(s => s.Position);
            foreach (var pos in dhPositions)
                if (counts.ContainsKey(pos))
                    counts[pos]++;
                else
                    counts.Add(pos, 1);
            PositionStatusList.Clear();
            foreach (var pos in Position.All)
            {
                int count = counts.TryGetValue(pos, out int value) ? value : 0;

                var ps = new PositionStatus
                {
                    PositionString = pos.PositionString,
                    StatusColor = count == 0 ? Colors.DimGray : // Missing
                                  count == 1 ? Colors.Green :   // Perfect
                                  Colors.Red                    // Conflict (Too many)
                };
                if (pos.Equals(Position.EH) && count > 1)
                    ps.StatusColor = Colors.Green;
                PositionStatusList.Add(ps);
            }
        }
        public void UpdatePositionLists()
        {
            if (CoreTeam is null)
                return;
            var roster = new List<Player>();
            if (ApiService.ApiRosters.TryGetValue(CoreTeam.Name, out var apiRoster))
            {
                roster = apiRoster.Select(s => new Player(s.LastName, s.Number.ToString()) { FirstName = s.FirstName }).ToList();
            }
            else
            {
                roster = [.. CoreTeam.Roster];
            }
            foreach (var player in roster.Except(CoreTeam.Lineup))
                TeamPlayers.Add(player);
        }
        public async Task LoadRoster(ScoreboardApi.Models.Team team)
        {
            if (team is null)
                return;
            TeamPlayers.Clear();
            var roster = await _vm.ApiService.GetRosterFromApi(team, true);
            var newTeam = Team.Create(team.Name, [.. roster.Select(s => Player.Create(s.Number, s.FirstName, s.LastName))]);
            if (roster.Any())
                ApiService.ApiRosters[team.Name] = roster;
            CoreTeam.SetRoster(newTeam.Roster);
            foreach (var player in newTeam.Roster)
                TeamPlayers.Add(player);
        }

        public ICommand SubstitutePlayerCommand => new Command<LineupPosition>(async (replaced) => await _vm.RosterCoordinator.SubstitutePlayer(_isHome, replaced));
        public ICommand SetLineupCommand => new RelayCommand(() =>
        {
            IsEditing = true;
            TeamPlayers.Clear();
            UpdatePositionLists();
            UpdatePositionAvailability();
        });
        public ICommand SelectPitcherCommand => new RelayCommand(async () =>
        {
            await _vm.RosterCoordinator.SelectPitcher(_isHome);
            OnPropertyChanged(nameof(PitcherName));
        });
        public ICommand SetStatsCommand => new Command(() => _vm.GameCoordinator.SetStats(_isHome));
        public ICommand RefreshRosterCommand => new Command(async () =>
        {
            var team = _vm.ApiTeams.Single(s => s.Name == CoreTeam.Name);
            await LoadRoster(team);
        });

        public ICommand CloseSetLineupCommand => new RelayCommand(() => _vm.RosterCoordinator.CloseSetLineup(this));
        public ICommand AddToLineupCommand => new RelayCommand<Player>(async (player) => await _vm.RosterCoordinator.AddToLineup(this, player));
        public ICommand RemoveFromLineupCommand => new Command<LineupPosition>((lp) => _vm.RosterCoordinator.RemoveFromLineup(this, lp));
        public ICommand LineupItemDraggedCommand => new Command<LineupPosition>(_vm.RosterCoordinator.SetDraggedLineupPosition);
        public ICommand LineupItemDroppedCommand => new Command<LineupPosition>((lp) => _vm.RosterCoordinator.LineupItemDropped(this, lp));
        public bool IsSideBarOpen => _vm.IsSideBarOpen;

        private ScorebookViewModel _vm;
        private bool _isUnknownRoster;
        private bool _pitcherSelected;
        private bool _isHome;
        private string? _mobileText;
        private bool _isEditing;
    }
}
