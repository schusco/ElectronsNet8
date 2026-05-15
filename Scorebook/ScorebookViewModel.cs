using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using ScoreboardApi.Models;
using Scorebook.Coordinators;
using Scorebook.Messages;
using Scorebook.Services;
using Scorebook.ViewObjects;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CoreTeam = Electrons.Core.Net8.Games.Team;
using Team = ScoreboardApi.Models.Team;

namespace Scorebook
{
    public class ScorebookViewModel : INotifyPropertyChanged
    {
        public const string FinalText = "Final";
        private GameUpdateManager _gameManager;
        public ScorebookViewModel(ApiService apiService, RosterCoordinator rosterCoordinator, GameCoordinator gameCoordinator, GameUpdateManager gameManager)
        {
            WeakReferenceMessenger.Default.Register<PositionChangedMessage>(this, rosterCoordinator.HandlePositionChangedMesage);
            _apiService = apiService;
            _rosterCoordinator = rosterCoordinator;
            _gameCoordinator = gameCoordinator;
            GameSelection = new GameSelection(this);
            ShowGameSelectionOptions = true;
            IsSideBarOpen = true;
            _gameManager = gameManager;
        }
        public BaseballGame? Game
        {
            get => _game;
            set
            {
                if (value == null)
                    return;
                _game = value;
                _game.ScoreChanged += _gameCoordinator.ScoreChanged;
                _rosterCoordinator.ViewModel = this;
                _gameCoordinator.ViewModel = this;
                if (_game.CurrentInning != null)
                    foreach (var ev in _game.CurrentInning.Events.Reverse())
                        InningEvents.Add(ev.ToString());
                CurrentAb = _game?.CurrentAb;
                UpdatePitches();
                if (CurrentAb != null)
                    CurrentAb.ScoringUpdated += _gameCoordinator.ScoringUpdated;
                _game.InningStarted += _gameCoordinator.InningStarted;
                _game.InningEnded += _gameCoordinator.InningEnded;
                _game.GameEnded += _gameCoordinator.GameEnded;
                HomeTeam?.UpdatePositionLists();
                AwayTeam?.UpdatePositionLists();
                IsBottomHalfOfInning = !_game.IsGameOver && _game.CurrentInning?.Half == HalfInning.Bottom;
                IsTopHalfOfInning = !_game.IsGameOver && _game.CurrentInning?.Half == HalfInning.Top;
                InningNumber = _game.IsGameOver ? FinalText : _game.CurrentInning?.Number.ToString() ?? "";
                OnPropertyChanged(nameof(Game));
                ShowGameSelectionOptions = false;
                UpdateScoreBoard();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public GameCoordinator GameCoordinator => _gameCoordinator;
        public GameUpdateManager GameManager => _gameManager;
        public RosterCoordinator RosterCoordinator => _rosterCoordinator;
        public ApiService ApiService => _apiService;
        public GameSelection GameSelection { get; set; }
        public DefensiveAlignment Defense
        {
            get => _defense;
            set
            {
                _defense = value;
                OnPropertyChanged(nameof(Defense));
            }
        }
        public AtBat? CurrentAb
        {
            get => _currentAb;
            set
            {
                _currentAb = value;
                OnPropertyChanged(nameof(CurrentAb));
                var list = from inning in _game.Innings
                           from ab in inning.Events
                           where ab.IsFinished && ab.Batter == _game.CurrentAb.Batter
                           select $"{inning.Number.NumberString()}) {ab.Result}";
                PreviousAtBats.Clear();
                foreach (var ab in list)
                    PreviousAtBats.Add(ab);
            }
        }
        public bool ShowLineScore
        {
            get => _showLineScore;
            set
            {
                _showLineScore = value;
                OnPropertyChanged(nameof(ShowLineScore));
            }
        }
        public int TotalInningCount
        {
            get => _totalInningCount;
            set
            {
                _totalInningCount = value;
                OnPropertyChanged(nameof(TotalInningCount));
            }
        }
        public bool SaveAwarded
        {
            get => _saveAwarded;
            set
            {
                _saveAwarded = value;
                OnPropertyChanged(nameof(SaveAwarded));
            }
        }
        public string SaveAwardedTo
        {
            get => _saveAwardedTo;
            set
            {
                _saveAwardedTo = value;
                OnPropertyChanged(nameof(SaveAwardedTo));
            }
        }
        public bool ShowStats
        {
            get => _showStats;
            set
            {
                _showStats = value;
                OnPropertyChanged(nameof(ShowStats));
            }
        }
        public bool IsGameStarted
        {
            get => _isGameStarted;
            set
            {
                if (_isGameStarted != value)
                {
                    _isGameStarted = value;
                    OnPropertyChanged(nameof(IsGameStarted));
                    OnPropertyChanged(nameof(NextBatterText));
                    OnPropertyChanged(nameof(ShowFieldPositionLinks));
                }
            }
        }
        public bool ScoringIsRequired
        {
            get => _scoringIsRequired;
            set
            {
                _scoringIsRequired = value;
                OnPropertyChanged(nameof(ScoringIsRequired));
                OnPropertyChanged(nameof(NextBatterText));
            }
        }
        public string NextBatterText
        {
            get
            {
                if (!IsGameStarted)
                    return "Start Game";
                if (ScoringIsRequired)
                    return "Add Scoring";
                return "Next Batter";
            }
        }
        public bool IsGameNull => Game == null;
        public FieldLocation? ActiveHitZone
        {
            get => _activeHitZone;
            set
            {
                _activeHitZone = value;
                OnPropertyChanged(nameof(ActiveHitZone));
            }
        }
        public string RunnerOnFirst => GetRunner(OnBase.First);
        public string RunnerOnSecond => GetRunner(OnBase.Second);
        public string RunnerOnThird => GetRunner(OnBase.Third);
        public bool RunnerOnFirstIsOut
        {
            get => _runnerOnFirstIsOut;
            set
            {
                _runnerOnFirstIsOut = value;
                OnPropertyChanged(nameof(RunnerOnFirstIsOut));
            }
        }
        public bool RunnerOnSecondIsOut
        {
            get => _runnerOnSecondIsOut;
            set
            {
                _runnerOnSecondIsOut = value;
                OnPropertyChanged(nameof(RunnerOnSecondIsOut));
            }
        }
        public bool RunnerOnThirdIsOut
        {
            get => _runnerOnThirdIsOut;
            set
            {
                _runnerOnThirdIsOut = value;
                OnPropertyChanged(nameof(RunnerOnThirdIsOut));
            }
        }
        public bool SendRunnerBackButtonVisible
        {
            get => _sendRunnerBackButtonVisible;
            set
            {
                _sendRunnerBackButtonVisible = value;
                OnPropertyChanged(nameof(SendRunnerBackButtonVisible));
            }
        }
        public bool ShowDefensiveAlignment
        {
            get => _showDefensiveAlignment;
            set
            {
                _showDefensiveAlignment = value;
                OnPropertyChanged(nameof(ShowDefensiveAlignment));
                OnPropertyChanged(nameof(ShowFieldPositionLinks));
            }
        }
        public bool IsSideBarOpen
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    return true;
                return _isSideBarOpen;
            }
            set
            {
                if (Game is null)
                    value = false;
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    _isSideBarOpen = true;
                else
                    _isSideBarOpen = value;
                OnPropertyChanged(nameof(IsSideBarOpen));
                OnPropertyChanged(nameof(ShowMainArea));
                OnPropertyChanged(nameof(ShowLineupBackGround));
            }
        }
        public bool ShowMainArea
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    return true;
                return !IsSideBarOpen;
            }
        }
        public bool ShowLineupBackGround => !ShowMainArea;
        public bool ShowFieldPositionLinks
        {
            get => _isGameStarted && !_showDefensiveAlignment;
        }
        public int CurrentBalls => CurrentAb?.Balls ?? 0;
        public int CurrentStrikes => CurrentAb?.Strikes ?? 0;
        public int CurrentOuts => Game?.CurrentInning?.Outs ?? 0;
        public int CurrentRbis
        {
            get => _currentRbis;
            set
            {
                _currentRbis = value;
                Game?.SetRunsBattedInForAb(value);
                OnPropertyChanged(nameof(CurrentRbis));
            }
        }
        public bool IsPitchesPanelVisible
        {
            get => _isPitchesPanelVisible;
            set
            {
                _isPitchesPanelVisible = value;
                OnPropertyChanged();
            }
        }
        public bool IsFieldOverlayVisible
        {
            get => _isFieldOverlayVisible;
            set
            {
                _isFieldOverlayVisible = value;
                OnPropertyChanged(nameof(IsFieldOverlayVisible));
            }
        }
        public bool GameIsOver
        {
            get => _gameIsOver;
            set
            {
                _gameIsOver = value;
                OnPropertyChanged(nameof(GameIsOver));
            }
        }
        public bool ShowGameSelectionOptions
        {
            get => _showGameSelectionOptions;
            set
            {
                _showGameSelectionOptions = value;
                OnPropertyChanged(nameof(ShowGameSelectionOptions));
            }
        }
        public TeamWrapper? AwayTeam
        {
            get => _awayTeam;
            set
            {
                _awayTeam = value;
                OnPropertyChanged(nameof(AwayTeam));
            }
        }
        public TeamWrapper? HomeTeam
        {
            get => _homeTeam;
            set
            {
                _homeTeam = value;
                OnPropertyChanged(nameof(HomeTeam));
            }
        }
        public bool EditingHomeLineup => HomeTeam?.IsEditing ?? false;
        public bool EditingAwayLineup => AwayTeam?.IsEditing ?? false;
        public bool ShowDesktopActionButtons
        {
            get => _showDesktopActionButtons;
            set
            {
                _showDesktopActionButtons = value;
                OnPropertyChanged(nameof(ShowDesktopActionButtons));
            }
        }
        public bool MobileActionTrigger
        {
            get => _mobileActionTrigger;
            set
            {
                _mobileActionTrigger = value;
                OnPropertyChanged(nameof(MobileActionTrigger));
            }
        }
        public bool ShowActionDialog
        {
            get => _showActionDialog;
            set
            {
                _showActionDialog = value;
                OnPropertyChanged(nameof(ShowActionDialog));
            }
        }
        public string InningNumber
        {
            get => _inningNumber;
            set
            {
                _inningNumber = value;
                OnPropertyChanged(nameof(InningNumber));
            }
        }
        public bool IsTopHalfOfInning
        {
            get => _isTopHalfOfInning;
            set
            {
                _isTopHalfOfInning = value;
                OnPropertyChanged(nameof(IsTopHalfOfInning));
            }
        }
        public bool IsBottomHalfOfInning
        {
            get => _isBottomHalfOfInning;
            set
            {
                _isBottomHalfOfInning = value;
                OnPropertyChanged(nameof(IsBottomHalfOfInning));
            }
        }
        public PitchTotals CurrentPitchStats
        {
            get
            {
                if (_currentPitchStats is null)
                    return PitchTotals.Blank;
                return _currentPitchStats;

            }
            set
            {
                _currentPitchStats = value;
                OnPropertyChanged(nameof(CurrentPitchStats));
            }
        }
        public ObservableCollection<string> InningEvents { get; set; } = [];
        public ObservableCollection<string> CurrentAbPitches { get; set; } = [];
        public ObservableCollection<StatsRow<HStats>> GameHittingStats { get; set; } = [];
        public ObservableCollection<StatsRow<PStats>> GamePitchingStats { get; set; } = [];
        public ObservableCollection<LineScoreData> LineScore { get; set; } = [];
        public ObservableCollection<string> PreviousAtBats { get; set; } = [];
        public static bool TeamsAreLoaded => ApiService.ApiTeams?.Count != 0;
        internal void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        internal void UpdatePitches()
        {
            if (CurrentAb != null)
            {
                CurrentAbPitches.Clear();
                CurrentAbPitches.Add($"{CurrentAb.Pitcher.LastName} pitching to {CurrentAb.Batter.LastName}");
                foreach (var pitch in CurrentAb.Pitches)
                    CurrentAbPitches.Add($"{pitch.Sequence}) {pitch}");
                var stats = _gameCoordinator.GetCurrentPitcherStats();
                CurrentPitchStats = new PitchTotals(stats);
            }
        }
        private void UpdateScoreBoard()
        {
            OnPropertyChanged(nameof(CurrentBalls));
            OnPropertyChanged(nameof(CurrentStrikes));
            OnPropertyChanged(nameof(CurrentOuts));
        }
        internal void LoadGame(string loadPath)
        {
            var game = BaseballGame.Load(loadPath);
            Game = game;            
            GameLoaded();
            IsGameStarted = game.IsStarted;
            HomeTeam = new TeamWrapper(this, game.HomeTeam, true);
            AwayTeam = new TeamWrapper(this, game.AwayTeam, false);
            HomeTeam.FillLineup(true);
            AwayTeam.FillLineup(true);
        }
        internal void GameLoaded()
        {
            OnPropertyChanged(nameof(Game));
            OnPropertyChanged(nameof(IsGameNull));
            UpdateRunners();
        }
        internal void UpdateRunners()
        {
            var oobs = CurrentAb?.Result?.Events?.OfType<OutOnBases>() ?? new List<OutOnBases>();

            RunnerOnFirstIsOut = oobs.Any(a => a.OutAt == OnBase.First);
            RunnerOnSecondIsOut = oobs.Any(a => a.OutAt == OnBase.Second);
            RunnerOnThirdIsOut = oobs.Any(a => a.OutAt == OnBase.Third);
            OnPropertyChanged(nameof(RunnerOnFirst));
            OnPropertyChanged(nameof(RunnerOnSecond));
            OnPropertyChanged(nameof(RunnerOnThird));
        }
        public void LinkAb()
        {
            if (InningEvents.Any())
            {
                InningEvents.RemoveAt(0);
                InningEvents.Insert(0, CurrentAb?.ToString() ?? "");
            }
            if (Game?.CurrentAb != null && Game.CurrentAb != CurrentAb)
            {
                CurrentAb = Game?.CurrentAb;
                InningEvents.Insert(0, CurrentAb?.ToString() ?? "");
            }

            var lineup = Game?.CurrentInning.Half == HalfInning.Top ? AwayTeam.Lineup : HomeTeam.Lineup;
            foreach (var lp in lineup)
                lp.IsActive = false;
            var current = lineup.SingleOrDefault(s => s.Player == CurrentAb?.Batter);
            if (current is not null)
                current.IsActive = true;
            var pitcherText = $"Pitching: {CurrentAb?.Pitcher?.FullName}";
            var hitterText = $"Batting: {CurrentAb?.Batter?.FullName}";
            if (Game?.CurrentInning?.Half == HalfInning.Bottom)
            {
                AwayTeam.MobileText = pitcherText;
                HomeTeam.MobileText = hitterText;
            }
            else
            {
                HomeTeam.MobileText = pitcherText;
                AwayTeam.MobileText = hitterText;
            }
            CurrentRbis = 0;
            UpdatePitches();
            UpdateScoreBoard();
            OnPropertyChanged(nameof(Game));
            UpdateRunners();
        }
        public void ReplaceCurrentAbInLog()
        {
            InningEvents.RemoveAt(0);
            InningEvents.Insert(0, $"{CurrentAb?.ToString()}");
        }
        public ICommand ShowInningLogCommand => new Command(async () =>
        {
            await Application.Current?.MainPage?.DisplayAlert("Inning Log", string.Join("\n\n", InningEvents), "OK");
        });
        public ICommand ShowCurrentAbCommand => new Command(async () =>
        {
            if (CurrentAb != null)
                await Application.Current?.MainPage?.DisplayAlert("Current At Bat", string.Join("\n\n", CurrentAb.Events.Select(s => $"{s.Sequence}) {s.ToString()}")), "OK");
        });
        public ICommand ShowPreviousAbsCommand => new Command(async () =>
        {
            if (CurrentAb != null)
                await Application.Current?.MainPage?.DisplayAlert("Previous At Bats", string.Join("\n\n", PreviousAtBats), "OK");
        });
        public ICommand ShowPitchTotalsCommand => new Command(async () =>
        {
            if (CurrentPitchStats != null)
                await Application.Current?.MainPage?.DisplayAlert("Current Pitch Totals", $"{CurrentPitchStats.PlayerName}\n\nStrikes: {CurrentPitchStats.Strikes}\nBalls: {CurrentPitchStats.Balls}\nTotal: {CurrentPitchStats.Total}", "OK");
        });
        public ICommand ToggleSidebarCommand => new Command(() => IsSideBarOpen = !IsSideBarOpen);
        public ICommand PrevBatterNavigationCommmand => new Command(() =>
        {
            Game.PreviousAtBat();
            CurrentAb = Game.CurrentAb;
            InningEvents.RemoveAt(0);
        });
        public ICommand TogglePitchesCommand => new RelayCommand(() => IsPitchesPanelVisible = !IsPitchesPanelVisible);
        public ICommand NextBatterCommand => new Command(async () => await _gameCoordinator.NextBatter(Game));
        public ICommand ShowOtherMenuCommand => new Command(async () => await _gameCoordinator.ShowOtherMenu(Game));
        public ICommand ViewDefenseCommand => new Command(() => ShowDefensiveAlignment = !ShowDefensiveAlignment);
        public ICommand ScoringEnteredCommand => new Command<AB>(async (ab) => await _gameCoordinator.ScoringEntered(ab, Game));
        public ICommand AddPitchCommand => new Command<PitchResult>(_gameCoordinator.AddPitch);
        public ICommand PositionLinkCommand => new Command<Position>((pos) =>
        {
            CurrentAb.Result.AddFielder(pos);
            OnPropertyChanged(nameof(CurrentAb));
        });
        public ICommand UndoCommand => new Command(() =>
        {
            Game.UndoScoring();
            UpdateRunners();
            ReplaceCurrentAbInLog();
            UpdatePitches();
            OnPropertyChanged(nameof(CurrentAb));
            UpdateScoreBoard();
            IsFieldOverlayVisible = false;
        });
        public ICommand SaveCommand => new Command(async () =>
        {
            if (Game is null)
                return;
            string fileName = $"{Game.HomeTeam.Name}{Game.AwayTeam.Name}{Game.GameDate.ToString("Md")}.sbg";
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                string localPath = Path.Combine(LocalSavePath, fileName);
                Game.SaveAs(localPath);
                await Application.Current.MainPage.DisplayAlert("Saved", "Game progress saved to device.", "OK");
            }
            else
            {
                string mainDir = FileSystem.CacheDirectory;
                var filePath = Path.Combine(mainDir, fileName);
                Game.SaveAs(filePath);
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Upload Game To Cloud",
                    File = new ShareFile(filePath, "text/plain")
                });
            }
        });
        public ICommand SendRunnerBackCommand => new Command(() =>
        {
            CurrentAb.UndoRunScored();
            UpdateRunners();
            SendRunnerBackButtonVisible = false;
        });
        public ICommand ShowCommandsCommand => new Command(() => ShowActionDialog = true);
        public ICommand CloseActionDialogCommand => new Command(() => ShowActionDialog = false);
        public ICommand RecordHitCommand => new Command<FieldLocation>(async (loc) =>
        {
            ActiveHitZone = loc;
            await Task.Delay(600);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (CurrentAb?.Result is Hit)
                    Game?.SetCurrentAbFieldLocation(loc);
                IsFieldOverlayVisible = false;
                ActiveHitZone = null;
            });
        });
        public ICommand CloseStatsViewCommand => new Command(() => { ShowStats = false; });
        public ICommand CloseLineScoreCommand => new Command(() => { ShowLineScore = false; });
        public ICommand ToggleGameSelectionCommand => new Command(() => { ShowGameSelectionOptions = !ShowGameSelectionOptions; });
        private string GetRunner(OnBase onBase)
        {
            var runners = Game?.CurrentInning?.CurrentRunners;
            if (runners != null && runners[onBase] != null)
                return runners[onBase].Runner.FullName;
            if (CurrentAb?.Result != null && CurrentAb.Result.Events.OfType<OutOnBases>().Any(a => a.OutAt == onBase))
            {
                return CurrentAb.Result.Events.OfType<OutOnBases>().First().Player.FullName;
            }
            return "";
        }
        internal async Task SetTeamsForGame(Team homeTeam, Team awayTeam)
        {
            var roster = await ApiService.GetRosterFromApi(homeTeam);
            if (roster != null && roster.Any())
                SetHomeTeam(roster, homeTeam.Name);
            else
            {
                Game?.SetHomeTeam(CoreTeam.CreateWithUnknownRoster(homeTeam.Name));
                HomeTeam = new TeamWrapper(this, Game.HomeTeam, true, true);
            }
            roster = await ApiService.GetRosterFromApi(awayTeam);
            if (roster != null && roster.Any())
                SetAwayTeam(roster, awayTeam.Name);
            else
            {
                Game.SetAwayTeam(CoreTeam.CreateWithUnknownRoster(awayTeam.Name));
                AwayTeam = new TeamWrapper(this, Game.AwayTeam, false, true);
            }
        }
        private void SetHomeTeam(List<CmbaPlayer> hroster, string name, bool unknown = false)
        {
            var team = CoreTeam.Create(name, hroster.Select(s => Player.Create(s.Number, s.FirstName, s.LastName)).ToList());
            Game.SetHomeTeam(team);
            HomeTeam = new TeamWrapper(this, team, true, unknown);
        }
        private void SetAwayTeam(List<CmbaPlayer> aroster, string name, bool unknown = false)
        {
            var team = CoreTeam.Create(name, aroster.Select(s => Player.Create(s.Number, s.FirstName, s.LastName)).ToList());
            Game.SetAwayTeam(team);
            AwayTeam = new TeamWrapper(this, team, false, unknown);
        }
        public async Task LoadTeamsAndLeagues()
        {
            var teams = await _apiService.GetTeams();
            foreach (var team in teams)
                ApiService.ApiTeams.Add(team);
            foreach (var league in teams.Select(t => t.Division).Distinct())
                GameSelection.Leagues.Add(league);
        }
        public async Task LoadSchedule(int teamId)
        {
            foreach (var game in await _apiService.GetSchedule(teamId))
                GameSelection.Schedule.Add(game);
        }
        public string LocalSavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Baseball", DateTime.Now.Year.ToString(), "Game Files");
        private BaseballGame? _game;
        private AtBat? _currentAb;
        private ApiService _apiService;
        private RosterCoordinator _rosterCoordinator;
        private GameCoordinator _gameCoordinator;
        private TeamWrapper? _homeTeam;
        private TeamWrapper? _awayTeam;
        private bool _isPitchesPanelVisible;
        private bool _showGameSelectionOptions;
        private bool _isGameStarted;
        private int _currentRbis;
        private bool _scoringIsRequired;
        private bool _runnerOnFirstIsOut;
        private bool _runnerOnSecondIsOut;
        private bool _runnerOnThirdIsOut;
        private bool _sendRunnerBackButtonVisible;
        private bool _showDefensiveAlignment;
        private bool _isFieldOverlayVisible;
        private bool _saveAwarded;
        private bool _isSideBarOpen;
        private string _saveAwardedTo = "";
        private bool _showStats;
        private bool _gameIsOver;
        private int _totalInningCount;
        private bool _showLineScore;
        private bool _showDesktopActionButtons = true;
        private bool _mobileActionTrigger = false;
        private bool _showActionDialog = false;
        private string? _inningNumber;
        private bool _isTopHalfOfInning;
        private bool _isBottomHalfOfInning;
        private FieldLocation? _activeHitZone;
        private PitchTotals? _currentPitchStats;
        private DefensiveAlignment? _defense;
    }
}
