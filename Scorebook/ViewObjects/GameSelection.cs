using Electrons.Core.Net8.Games;
using ScoreboardApi.Models;
using Scorebook.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Team = ScoreboardApi.Models.Team;
namespace Scorebook.ViewObjects
{
    public class GameSelection(ScorebookViewModel vm) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public GameScoreWrapper? SelectedGame { get; set; }
        public Team? SelectedHomeTeam
        {
            get => _selectedHomeTeam;
            set
            {
                _selectedHomeTeam = value;
                OnPropertyChanged(nameof(SelectedHomeTeam));
                OnPropertyChanged(nameof(CanStartGame));
                ((Command)CreateGameCommand).ChangeCanExecute();
            }
        }
        public Team? SelectedAwayTeam
        {
            get => _selectedAwayTeam;
            set
            {
                _selectedAwayTeam = value;
                OnPropertyChanged(nameof(SelectedAwayTeam));
                OnPropertyChanged(nameof(CanStartGame));
                ((Command)CreateGameCommand).ChangeCanExecute();
            }
        }
        public bool IsConfiguringNewGame
        {
            get => _isConfiguringNewGame;
            set
            {
                _isConfiguringNewGame = value;
                OnPropertyChanged(nameof(IsConfiguringNewGame));
                OnPropertyChanged(nameof(ShowCancelSelectionBox));
            }
        }
        public bool IsSelectingGameFromSchedule
        {
            get => _isSelectingGameFromSchedule;
            set
            {
                _isSelectingGameFromSchedule = value;
                OnPropertyChanged(nameof(IsSelectingGameFromSchedule));
                OnPropertyChanged(nameof(ShowCancelSelectionBox));
            }
        }
        public bool ShowCancelSelectionBox => IsSelectingGameFromSchedule || IsConfiguringNewGame;
        public string? SelectedHomeLeague
        {
            get => _selectedHomeLeague;
            set
            {
                if (_selectedHomeLeague != value)
                {
                    _selectedHomeLeague = value;
                    OnPropertyChanged(nameof(SelectedHomeLeague));
                    FilterTeams(true);
                }
            }
        }
        public string? SelectedAwayLeague
        {
            get => _selectedAwayLeague;
            set
            {
                if (_selectedAwayLeague != value)
                {
                    _selectedAwayLeague = value;
                    OnPropertyChanged(nameof(SelectedAwayLeague));
                    FilterTeams(false);
                }
            }
        }
        public bool CanStartGame => SelectedHomeTeam != null && SelectedAwayTeam != null;
        public ObservableCollection<Team> FilteredHomeTeams { get; set; } = [];
        public ObservableCollection<Team> FilteredAwayTeams { get; set; } = [];
        public ObservableCollection<string> Leagues { get; set; } = [];
        public ObservableCollection<GameScore> Schedule { get; set; } = [];
        public ObservableCollection<GameScoreWrapper> GameScores { get; set; } = [];
        public void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        internal void FilterTeams(bool? home = null)
        {
            if (!home.HasValue || home.Value)
            {
                FilteredHomeTeams.Clear();
                if (SelectedHomeLeague != null)
                {
                    foreach (var team in ApiService.ApiTeams.Where(w => w.Division == SelectedHomeLeague))
                        FilteredHomeTeams.Add(team);
                }
                else
                {
                    foreach (var team in ApiService.ApiTeams)
                        FilteredHomeTeams.Add(team);
                }
            }
            if (!home.HasValue || !home.Value)
            {
                FilteredAwayTeams.Clear();
                if (SelectedAwayLeague != null)
                {
                    foreach (var team in ApiService.ApiTeams.Where(w => w.Division == SelectedAwayLeague))
                        FilteredAwayTeams.Add(team);
                }
                else
                {
                    foreach (var team in ApiService.ApiTeams)
                        FilteredAwayTeams.Add(team);
                }
            }
        }
        public ICommand CreateGameFromScheduleCommand => new Command(async () =>
        {
            var homeLeague = "CMBA";
            var homeTeam = ApiService.ApiTeams.FirstOrDefault(f => f.Name == SelectedGame?.HomeTeam?.Name);
            var awayTeam = ApiService.ApiTeams.FirstOrDefault(f => f.Name == SelectedGame?.AwayTeam?.Name);
            if (homeTeam == null || awayTeam == null)
                return;
            var innings = _leagueDict.ContainsKey(homeLeague) ? _leagueDict[homeLeague] : 7;
            _vm.Game = new BaseballGame(innings);
            await _vm.SetTeamsForGame(homeTeam, awayTeam);
            IsSelectingGameFromSchedule = false;
            _vm.GameLoaded();
        });
        public ICommand CreateGameCommand => new Command(async () =>
        {
            var homeTeam = ApiService.ApiTeams.FirstOrDefault(f => f.Name == SelectedHomeTeam?.Name);
            var awayTeam = ApiService.ApiTeams.FirstOrDefault(f => f.Name == SelectedAwayTeam?.Name);
            if (homeTeam == null || awayTeam == null)
                return;
            var innings = _leagueDict.ContainsKey(SelectedHomeLeague) ? _leagueDict[SelectedHomeLeague] : 7;
            _vm.Game = new BaseballGame(innings);
            await _vm.SetTeamsForGame(homeTeam, awayTeam);
            IsConfiguringNewGame = false;
            _vm.GameLoaded();
        }, () => SelectedHomeTeam != null && SelectedAwayTeam != null);
        public ICommand SelectFromScheduleCommand => new Command(() =>
        {
            if (Schedule.Count != 0 && GameScores.Count == 0)
            {
                foreach (var game in Schedule.Where(w => w.GameDate > DateTime.Today))
                    GameScores.Add(GameScoreWrapper.Create(game));
            }
            IsSelectingGameFromSchedule = true;
        });
        public ICommand LoadGameCommand => new Command(async () =>
        {
            try
            {
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.Android, new[] {"application.json","application.xml"} },
                        { DevicePlatform.WinUI,new[]{".json",".xml",".sbg"} }
                    });
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select Electrons Game File",
                    FileTypes = customFileType
                });

                if (result != null)
                {
                    _vm.LoadGame(result.FullPath);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        });
        public ICommand ConfigureGameCommand => new Command(() => IsConfiguringNewGame = true);
        public ICommand CloseGameSelectionCommand => new Command(() => IsSelectingGameFromSchedule = IsConfiguringNewGame = false);

        private readonly ScorebookViewModel _vm = vm;
        private string? _selectedHomeLeague = "CMBA";
        private string? _selectedAwayLeague = "CMBA";
        private Team? _selectedHomeTeam;
        private Team? _selectedAwayTeam;
        private bool _isSelectingGameFromSchedule;
        private bool _isConfiguringNewGame;
        private readonly Dictionary<string, int> _leagueDict = new()
        {
            { "CMBA", 7 },
            { "BJL", 9 },
            { "CSYBL", 7 }
        };
    }
}
