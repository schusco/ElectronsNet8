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
    public class GameSelection(ScorebookViewModel vm, ApiService apiService) : INotifyPropertyChanged
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
        public bool SendGameUpdates
        {
            get => _sendGameUpdates;
            set
            {
                _sendGameUpdates = value;
                OnPropertyChanged(nameof(SendGameUpdates));
            }
        }
        public int? GameUpdateId => !SendGameUpdates || !_vm.GameManager.IsLoggedIn ? null : SelectedGame?.GameId;
        public DateTime? EndDateTime { get; set; }
        public DateTime? StartDateTime { get; set; }
        public bool IsGameStarted => _vm.IsGameStarted;
        public bool GameInProgress => _vm.IsGameStarted && !_vm.GameIsOver;
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
            await LoadGameData();
            if (SendGameUpdates && !_vm.GameManager.IsLoggedIn)
                await _vm.GameManager.StartAsync();
            if (GameUpdateId.HasValue)
            {
                SelectedGame.GameScoreUpdated += _vm.ApiService.SendGameUpdate;
                SelectedGame.InningUpdated += _vm.ApiService.SendInningUpdate;
                SelectedGame.AtbatUpdated += _vm.ApiService.SendAbUpdate;
                _vm.ApiService.InningCreated += SelectedGame.UpdateCurrentInning;
                _vm.ApiService.AbCreated += SelectedGame.UpdateCurrentAtbat;
                _vm.GameManager.SetSelectedGame(SelectedGame);
            }
        });
        private async Task LoadGameData()
        {
            var id = 0;
            if (SelectedGame is not null)
                id = SelectedGame.GameId;
            else
            {
                var game = Schedule.FirstOrDefault(s => s.HomeTeam?.Name == _vm.Game.HomeTeam.Name && s.AwayTeam.Name == _vm.Game?.AwayTeam.Name);
                if (game is not null)
                    id = game.GameId;
            }
            var apiGame = await _apiService.GetGame(id);
            StartDateTime = apiGame?.StartDateTime;
            EndDateTime = apiGame?.EndDateTime;
        }        
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
                foreach (var game in Schedule)
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
                        { DevicePlatform.iOS, new[] { "public.xml" } },
                        { DevicePlatform.Android, new[] {"*/*"} },
                        { DevicePlatform.WinUI,new[]{".xml",".sbg"} }
                    });
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select Game File",
                    FileTypes = customFileType
                });

                if (result != null)
                {
                    var fNameSplit = result.FileName.Split('.');
                    if (fNameSplit.Length > 1 && fNameSplit[1] == "sbg")
                    {
                        _vm.LoadGame(result.FullPath);
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Please select a valid sbg file.", "Ok");
                    }
                }
            }
            catch (Exception ex)
            {
                var dInfo = Directory.EnumerateFiles(_vm.LocalSavePath).Where(w => w.EndsWith(".sbg")).ToArray();
                var displayNames = dInfo.Select(s => Path.GetFileName(s)).ToArray();
                var fName = await Application.Current.MainPage.DisplayActionSheet("Select File", "Cancel", null, displayNames);
                if (fName == "Cancel")
                    return;
                var index = displayNames.ToList().IndexOf(fName);
                _vm.LoadGame(dInfo[index]);
            }
        });
        public ICommand ConfigureGameCommand => new Command(() => IsConfiguringNewGame = true);
        public ICommand CloseGameSelectionCommand => new Command(() => IsSelectingGameFromSchedule = IsConfiguringNewGame = false);
        public ICommand SaveCommand => new Command(async () => await _vm.GameCoordinator.SaveGame(_vm.Game));
        public ICommand EndGameCommand => new Command(() => _vm.Game.EndGame());

        private readonly ScorebookViewModel _vm = vm;
        private readonly ApiService _apiService = apiService;
        private string? _selectedHomeLeague = "CMBA";
        private string? _selectedAwayLeague = "CMBA";
        private Team? _selectedHomeTeam;
        private Team? _selectedAwayTeam;
        private bool _isSelectingGameFromSchedule;
        private bool _isConfiguringNewGame;
        private bool _sendGameUpdates;
        private readonly Dictionary<string, int> _leagueDict = new()
        {
            { "CMBA", 7 },
            { "BJL", 9 },
            { "CSYBL", 7 }
        };
    }
}
