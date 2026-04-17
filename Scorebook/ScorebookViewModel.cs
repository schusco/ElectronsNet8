using CommunityToolkit.Mvvm.Messaging;
using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using ScoreboardApi.Models;
using Scorebook.Messages;
using Scorebook.ViewObjects;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CoreTeam = Electrons.Core.Net8.Games.Team;
using Team = ScoreboardApi.Models.Team;

namespace Scorebook
{
    public class ScorebookViewModel : INotifyPropertyChanged
    {
        public ScorebookViewModel()
        {
            WeakReferenceMessenger.Default.Register<PositionChangedMessage>(this, HandlePositionChangedMesage);
            _selectedHomeLeague = "CMBA";
            _selectedAwayLeague = "CMBA";
        }

        public BaseballGame? Game
        {
            get => _game;
            set
            {
                if (value == null)
                    return;
                _game = value;
                _game.ScoreChanged += Game_ScoreChanged;
                if (_game.CurrentInning != null)
                    foreach (var ev in _game.CurrentInning.Events.Reverse())
                        InningEvents.Add(ev.ToString());
                CurrentAb = _game?.CurrentAb;
                UpdatePitches();
                if (CurrentAb != null)
                    CurrentAb.ScoringUpdated += AB_ScoringUpdated;
                _game.InningStarted += Game_InningStarted;
                _game.InningEnded += Game_InningEnded;
                _game.GameEnded += Game_GameEnded;
                UpdatePositionLists(_game.HomeTeam, true);
                UpdatePositionLists(_game.AwayTeam, false);
                OnPropertyChanged(nameof(Game));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private void Game_GameEnded(object? sender, EventArgs e)
        {
            GameIsOver = true;
            IsGameStarted = false;
            LineScore.Clear();
            TotalInningCount = new int[] { 7, Game.Innings.Max(m => m.Number) }.Max();
            var grps = Game?.Innings.GroupBy(g => g.Number);
            for (int i = 1; i <= TotalInningCount; i++)
                LineScore.Add(new LineScoreData(i, grps.SingleOrDefault(s => s.Key == i)));
            if (!(Game?.SaveAwardedTo is null))
            {
                SaveAwarded = true;
                SaveAwardedTo = Game.SaveAwardedTo.FullName;
            }
            OnPropertyChanged(nameof(Game));
            ShowLineScore = true;
        }
        private void Game_InningEnded(object? sender, InningChangeEventArgs e)
        {
            InningEvents.Clear();
            var stats = GetCurrentPitcherStats();
            CurrentPitchStats = new PitchTotals(stats);
        }
        private void Game_InningStarted(object? sender, InningChangeEventArgs e)
        {
            Defense.FieldingTeam = Game?.FieldingTeam;
            OnPropertyChanged(nameof(Defense));
        }
        private void AB_ScoringUpdated(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CurrentAb));
        }
        private void Game_ScoreChanged(object? sender, ScoreChangedEventArgs e)
        {
            var runs = e.RunnerAdvances?.Sum(s => s.Runs) ?? 0;
            CurrentRbis = runs;
            OnPropertyChanged(nameof(Game.HomeScore));
            OnPropertyChanged(nameof(Game.AwayScore));
        }
        private void UpdatePitches()
        {
            if (CurrentAb != null)
            {
                CurrentAbPitches.Clear();
                CurrentAbPitches.Add($"{CurrentAb.Pitcher.LastName} pitching to {CurrentAb.Batter.LastName}");
                foreach (var pitch in CurrentAb.Pitches)
                    CurrentAbPitches.Add($"{pitch.Sequence}) {pitch}");
                var stats = GetCurrentPitcherStats();
                CurrentPitchStats = new PitchTotals(stats);
            }
        }
        public void LoadGame(string loadPath)
        {
            var game = BaseballGame.Load(loadPath);
            Game = game;
            GameLoaded();
            IsGameStarted = game.IsStarted;
            FillLineup(HomeLineup, Game.HomeTeam);
            FillLineup(AwayLineup, Game.AwayTeam);
        }
        private void GameLoaded()
        {
            OnPropertyChanged(nameof(Game));
            OnPropertyChanged(nameof(IsGameNull));
            OnPropertyChanged(nameof(HomeTeamName));
            OnPropertyChanged(nameof(AwayTeamName));
            UpdateRunners();
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
        public bool HomePitcherSelected
        {
            get => _homePitcherSelected;
            set
            {
                _homePitcherSelected = value;
                OnPropertyChanged(nameof(HomePitcherSelected));
                OnPropertyChanged(nameof(HomePitcherText));
                OnPropertyChanged(nameof(HomePitcherName));
            }
        }
        public bool AwayPitcherSelected
        {
            get => _awayPitcherSelected;
            set
            {
                _awayPitcherSelected = value;
                OnPropertyChanged(nameof(AwayPitcherSelected));
                OnPropertyChanged(nameof(AwayPitcherText));
                OnPropertyChanged(nameof(AwayPitcherName));
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
        public string HomePitcherText => _homePitcherSelected ? "Change Pitcher" : "Set Pitcher";
        public string HomeSubText => Game?.HomeTeam?.OrderIsSet ?? false ? "View Lineup" : "Set Lineup";
        public string AwayPitcherText => _awayPitcherSelected ? "Change Pitcher" : "Set Pitcher";
        public string AwaySubText => Game?.AwayTeam?.OrderIsSet ?? false ? "View Lineup" : "Set Lineup";
        public string HomePitcherName => Game?.HomeTeam?.CurrentPitcher?.FullName ?? "Not Set";
        public string AwayPitcherName => Game?.AwayTeam?.CurrentPitcher?.FullName ?? "Not Set";
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
        public DefensiveAlignment Defense { get; set; } = new DefensiveAlignment();
        public string HomeTeamName => $"{Game?.HomeTeam?.Name} (Home)";
        public string AwayTeamName => $"{Game?.AwayTeam?.Name} (Away)";
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
            get => _isSideBarOpen;
            set
            {
                _isSideBarOpen = value;
                OnPropertyChanged(nameof(IsSideBarOpen));
            }
        }
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
        public GameScoreWrapper? SelectedGame { get; set; }
        public bool? EditingHomeLineup
        {
            get => _editingHomeLineup;
            set
            {
                _editingHomeLineup = value;
                OnPropertyChanged(nameof(EditingHomeLineup));
                OnPropertyChanged(nameof(ActiveLineup));
            }
        }
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
        public string SelectedHomeLeague
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
        public string SelectedAwayLeague
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
        public ObservableCollection<string> Leagues { get; set; } = [];
        public ObservableCollection<Team> FilteredHomeTeams { get; set; } = [];
        public ObservableCollection<Team> FilteredAwayTeams { get; set; } = [];
        public ObservableCollection<Player> TeamPlayers { get; set; } = [];
        public ObservableCollection<GameScoreWrapper> GameScores { get; set; } = [];
        public ObservableCollection<StatsRow<HStats>> GameHittingStats { get; set; } = [];
        public ObservableCollection<StatsRow<PStats>> GamePitchingStats { get; set; } = [];
        public ObservableCollection<LineScoreData> LineScore { get; set; } = [];
        public ObservableCollection<LineupPosition> HomeLineup { get; set; } = [];
        public ObservableCollection<LineupPosition> AwayLineup { get; set; } = [];
        public ObservableCollection<PositionStatus> PositionStatusList { get; set; } = [];
        public ObservableCollection<LineupPosition> ActiveLineup => EditingHomeLineup.GetValueOrDefault() ? HomeLineup : AwayLineup;
        public ObservableCollection<string> PreviousAtBats { get; set; } = [];
        public bool TeamsAreLoaded => _apiTeams?.Count != 0;
        public bool CanStartGame => SelectedHomeTeam != null && SelectedAwayTeam != null;
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
                InningEvents.Insert(0, CurrentAb.ToString());
            }
            if (Game?.CurrentAb != null && Game.CurrentAb != CurrentAb)
            {
                CurrentAb = Game?.CurrentAb;
                InningEvents.Insert(0, CurrentAb.ToString());
            }

            var lineup = Game?.CurrentInning.Half == HalfInning.Top ? AwayLineup : HomeLineup;
            foreach (var lp in lineup)
                lp.IsActive = false;
            var current = lineup.SingleOrDefault(s => s.Player == CurrentAb?.Batter);
            if (!(current is null))
                current.IsActive = true;
            UpdatePitches();
            OnPropertyChanged(nameof(CurrentBalls));
            OnPropertyChanged(nameof(CurrentStrikes));
            OnPropertyChanged(nameof(CurrentOuts));
            OnPropertyChanged(nameof(Game));
            UpdateRunners();
        }
        public void UpdatePositionLists(CoreTeam team, bool home)
        {
            if (team is null)
                return;
            foreach (var player in team.Roster.Except(team.Lineup))
                TeamPlayers.Add(player);
        }
        public void ReplaceCurrentAbInLog()
        {
            InningEvents.RemoveAt(0);
            InningEvents.Insert(0, $"{CurrentAb?.ToString()}");
        }
        private async Task<IList<RunningEvent>> HandleRunnerAdvances(int bases)
        {
            var advances = Game.AdvanceAllRunners(bases);
            if (advances.Any(a => a is RunScored))
            {
                advances = await CheckRunsScoredForEarned(advances);

            }
            return advances;
        }
        private async Task<IList<RunningEvent>> CheckRunsScoredForEarned(IList<RunningEvent> advances)
        {
            if (Game.CurrentInning.Errors > 0)
            {
                for (int i = 0; i < advances.Count; i++)
                {
                    var advance = advances[i];
                    var runner = Game.CurrentInning.CurrentRunners.RunnersOnBase.SingleOrDefault(s => s.Runner == advance.Player);
                    if (!(runner is null) && advance is RunScored && !runner.ReachedOnError)
                    {
                        var result = await ShowEarnedDialog(advance.Player);
                        if (!result)
                            advances[i] = RunScored.Unearned(advance);

                    }
                }
            }
            return advances;
        }
        private static async Task<bool> ShowEarnedDialog(Player runner)
        {
            return await Shell.Current.DisplayAlert("Earned Run", $"Errors occurred this inning, charge {runner.DisplayName}'s run as earned?", "Yes", "No");
        }
        internal async Task AdvanceRunners(OnBase onBase, AdvanceReason reason)
        {
            var runners = Game.CurrentInning.CurrentRunners;
            switch (onBase)
            {
                case OnBase.Third:
                    {
                        var chargeAsEarned = true;
                        if (Game.CurrentInning.Errors > 0 || Game.CurrentAb.Result.Errors > 0)
                            chargeAsEarned = await ShowEarnedDialog(runners.OnThird.Runner);
                        Game.AddEventToAb(Game.ScoreRunner(runners.OnThird, reason, chargeAsEarned));
                        break;
                    }
                case OnBase.Second:
                    Game.AddEventToAb(Game.AdvanceRunner(runners.OnSecond, OnBase.Third, reason));
                    break;
                case OnBase.First:
                    Game.AddEventToAb(Game.AdvanceRunner(runners.OnFirst, OnBase.Second, reason));
                    break;
            }

            OnPropertyChanged(nameof(CurrentAb));
            UpdateRunners();
        }
        private async Task UpdateCurrentAbResult(AB ab, IList<RunningEvent> advances)
        {
            if (await ShowNextBatterWarning())
                return;
            Game.UpdateCurrentAbResult(ab, advances);
        }
        private async Task<bool> ShowNextBatterWarning()
        {
            if (CurrentAb?.Result?.FinishedAb ?? false)
            {
                await Shell.Current.DisplayAlert("Warning", "Current AB is scored, undo to change.", "Ok");
                return true;
            }
            return false;
        }
        private async void HandlePositionChangedMesage(object recipient, PositionChangedMessage message)
        {
            if (message.Value != null)
            {
                var team = EditingHomeLineup.GetValueOrDefault() ? Game.HomeTeam : Game.AwayTeam;
                if (message.Value.Equals(Position.DH))
                {
                    var dhPlayerName = await Application.Current?.Windows[0]?.Page?.DisplayActionSheet("Select player to  DH for", "Cancel", null, team.Bench.Select(s => s.FullName).ToArray());
                    var dhPositionText = await Application.Current?.Windows[0]?.Page?.DisplayActionSheet("Select defensive position", "Cancel", null, [.. Position.All.Select(s => s.LongPositionString)]);
                    if (dhPlayerName is not null && dhPlayerName != "Cancel" && dhPositionText != "Cancel")
                    {
                        var player = ActiveLineup.FirstOrDefault(f => f.Position == Position.DH);
                        var dhPlayer = team.Roster.Single(s => s.FullName == dhPlayerName);
                        var dhPosition = Position.All.Single(s => s.LongPositionString == dhPositionText);
                        if (dhPosition == Position.P && !team.OrderIsSet)
                        {
                            team.SetStartingPitcher((Pitcher)dhPlayer);
                            UpdatePitcherUI(team);
                        }
                        dhPlayer.SetPosition(dhPosition);
                        player.HittingFor = dhPlayer;
                    }
                }
                if (message.Value.Equals(Position.P))
                {
                    var lp = ActiveLineup.FirstOrDefault(f => f.Position == Position.P);
                    if (lp != null && !team.OrderIsSet)
                    {

                        team.SetStartingPitcher((Pitcher)lp.Player);
                        UpdatePitcherUI(team);
                    }
                }
                UpdatePositionAvailability();
            }
        }
        private void UpdatePitcherUI(CoreTeam team)
        {
            if (team == Game?.HomeTeam)
                HomePitcherSelected = true;
            else
                AwayPitcherSelected = true;
        }
        private void FillLineup(ObservableCollection<LineupPosition> lineup, CoreTeam team)
        {
            var lp = 1;
            foreach (var player in team.Lineup)
                lineup.Add(new LineupPosition(player, lp++));
            UpdatePitcherUI(team);
        }
        public ICommand ToggleSidebarCommand => new Command(() => IsSideBarOpen = !IsSideBarOpen);
        public ICommand SelectPitcherCommand => new Command<bool>(async (home) =>
        {
            var team = home ? Game.HomeTeam : Game.AwayTeam;
            var allPitchers = team.AvailablePitchers.Select(s => s.FullName).OrderBy(o => o).ToArray();
            var pitcher = await Application.Current.Windows[0].Page.DisplayActionSheet("Select Pitcher", "Cancel", null, allPitchers);
            var selectedPitcher = team.AvailablePitchers.SingleOrDefault(s => s.FullName == pitcher);
            if (selectedPitcher is null || selectedPitcher == (Player)team.CurrentPitcher)
                return;
            if (team.CurrentPitcher is null || !team.OrderIsSet || !Game.IsStarted || (Game.IsStarted && team.CurrentPitcher.IsUnknown))
            {
                team.SetStartingPitcher((Pitcher)selectedPitcher);
                CurrentPitchStats = new PitchTotals(selectedPitcher.DisplayName);
                if (Game.IsStarted && selectedPitcher.IsMemberOf(Game.FieldingTeam))
                    Game.CurrentInning.SetCurrentPitcher((Pitcher)selectedPitcher);
                if (home)
                    HomePitcherSelected = true;
                else
                    AwayPitcherSelected = true;
                return;
            }
            var result = await Shell.Current.DisplayAlert("Confirm", $"Replace {team.CurrentPitcher.LastName} with {selectedPitcher.LastName}?  ", "Yes", "No");
            if (!result)
                return;
            var sub = Game.ChangePitcher(team, selectedPitcher);
            Game.AddEventToAb(sub);
            OnPropertyChanged(nameof(HomePitcherName));
            OnPropertyChanged(nameof(AwayPitcherName));
            OnPropertyChanged(nameof(CurrentAb));
            UpdatePitches();
        });
        public ICommand PrevBatterNavigationCommmand => new Command(() =>
        {
            Game.PreviousAtBat();
            CurrentAb = Game.CurrentAb;
            InningEvents.RemoveAt(0);
        });
        public ICommand ConfigureGameCommand => new Command(() => IsConfiguringNewGame = true);
        public ICommand LoadGameCommand => new Command(async () =>
        {
            try
            {
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        {DevicePlatform.Android, new[] {"application.json","application.xml"} },
                        { DevicePlatform.WinUI,new[]{".json",".xml",".sbg"} }
                    });
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select Electrons Game File",
                    FileTypes = customFileType
                });

                if (result != null)
                {
                    LoadGame(result.FullPath);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        });
        public ICommand TogglePitchesCommand => new Command(() => IsPitchesPanelVisible = !IsPitchesPanelVisible);
        public ICommand NextBatterCommand => new Command(async () =>
        {
            if (!IsGameStarted)
            {
                Game?.StartGame();
                IsGameStarted = true;
                OnPropertyChanged(nameof(Game));
                OnPropertyChanged(nameof(Defense));
                if (!HomeLineup.Any())
                    FillLineup(HomeLineup, Game.HomeTeam);
                if (!AwayLineup.Any())
                    FillLineup(AwayLineup, Game.AwayTeam);
                LinkAb();
            }
            else
            {
                IsFieldOverlayVisible = false;
                if (ScoringIsRequired)
                {
                    var scoringAdded = Game?.AddScoring();
                    ScoringIsRequired = false;
                    if (!CurrentAb.IsFinished && CurrentAb.Result is FieldersChoice fc)
                    {
                        var advances = Game.ForceRunners();
                        fc.AddAdvances(advances);
                        UpdateRunners();
                    }
                }
                else if (!Game?.FinishAb() ?? false)
                {
                    await Shell.Current.DisplayAlert("Error", "Add scoring before moving to next batter", "OK");
                    return;
                }

                if (!Game?.IsGameOver ?? false)
                    LinkAb();
            }
        });
        public ICommand UndoCommand => new Command(() =>
        {
            Game.UndoScoring();
            UpdateRunners();
            ReplaceCurrentAbInLog();
            UpdatePitches();
            OnPropertyChanged(nameof(CurrentAb));
            OnPropertyChanged(nameof(CurrentBalls));
            OnPropertyChanged(nameof(CurrentStrikes));
            OnPropertyChanged(nameof(CurrentOuts));
            IsFieldOverlayVisible = false;
        });
        public ICommand SaveCommand => new Command(async () =>
        {
            if (Game is null)
                return;
            string fileName = $"{Game.HomeTeam.Name}{Game.AwayTeam.Name}{Game.GameDate.ToString("Md")}.sbg";
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                string localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Baseball", DateTime.Now.Year.ToString(), "Game Files", fileName);
                Game.SaveAs(localPath);
                await Application.Current?.Windows[0]?.Page?.DisplayAlert("Saved", "Game progress saved to device.", "OK");
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
        public ICommand ShowOtherMenuCommand => new Command(async () =>
        {
            IList<RunningEvent>? advances = null;
            string action = await Application.Current?.Windows[0]?.Page?.DisplayActionSheet("Options", "Cancel",
                        null, "Balk", "Wild Pitch", "Passed Ball", "Sacrifice Fly", "Sac / Reached on Error", "Drop Third Strike");
            switch (action)
            {
                case "Balk":
                    Game?.AddEventToAb(AB.Balk, (Player)Game.CurrentAb.Pitcher, Game.AdvanceAllRunners(1, false));
                    break;
                case "Wild Pitch":
                case "Passed Ball":
                    var ab = action == "Wild Pitch" ? AB.WildPitch : AB.PassedBall;
                    advances = Game.AdvanceAllRunners(1, false);
                    Game.AddRunnerAdvances(ab, advances);
                    if (advances.Any(a => a is RunScored))
                        SendRunnerBackButtonVisible = true;
                    ReplaceCurrentAbInLog();
                    break;
                case "Sacrifice Fly":
                    await UpdateCurrentAbResult(AB.SacrificeFly, Game.AdvanceAllRunners(1, false));
                    break;
                case "Sac / Reached on Error":
                    await UpdateCurrentAbResult(AB.SacrificeReachOnError, Game.AdvanceAllRunners(1, true));
                    break;
                case "Drop Third Strike":
                    await UpdateCurrentAbResult(AB.DropThirdStrike, Game.ForceRunners());
                    break;
            }
            UpdateRunners();

        });
        public ICommand SendRunnerBackCommand => new Command(() =>
        {
            CurrentAb.UndoRunScored();
            UpdateRunners();
            SendRunnerBackButtonVisible = false;
        });
        public ICommand ViewDefenseCommand => new Command(() => ShowDefensiveAlignment = !ShowDefensiveAlignment);
        public ICommand ScoringEnteredCommand => new Command<AB>(async (ab) =>
        {
            if (await ShowNextBatterWarning())
                return;
            IList<RunningEvent>? advances = null;
            bool add = true;
            switch (ab)
            {
                case AB.Walk:
                case AB.HitByPitch:
                    advances = Game.ForceRunners();
                    break;
                case AB.Single:
                    advances = await HandleRunnerAdvances(1);
                    IsFieldOverlayVisible = true;
                    break;
                case AB.Double:
                    advances = await HandleRunnerAdvances(2);
                    IsFieldOverlayVisible = true;
                    break;
                case AB.Triple:
                    advances = await HandleRunnerAdvances(3);
                    IsFieldOverlayVisible = true;
                    break;
                case AB.HomeRun:
                    advances = await HandleRunnerAdvances(4);
                    IsFieldOverlayVisible = true;
                    break;
                case AB.StrikeOut:
                    var lastPitch = CurrentAb?.Pitches?.LastOrDefault();
                    if (!(lastPitch is null))
                    {
                        if (lastPitch.Result == PitchResult.CalledStrike)
                            ab = AB.StrikeOutLooking;
                        else if (lastPitch.Result == PitchResult.SwingingStrike)
                            ab = AB.StrikeOutSwinging;
                    }
                    break;
                case AB.ReachedOnError:
                    bool chargeEarned = false;
                    if (Game.CurrentInning.CurrentRunners.RunnerOnThird)
                        chargeEarned = await ScorebookViewModel.ShowEarnedDialog(Game.CurrentInning.CurrentRunners.OnThird.Runner);
                    advances = Game.ForceRunners(true, chargeEarned);
                    break;
                case AB.FieldersChoice:
                    ScoringIsRequired = true;
                    break;
                case AB.Sacrifice:
                    advances = Game.AdvanceAllRunners(1, false);
                    break;
                case AB.StolenBase:
                case AB.OutStealing:
                    if (!Game.CurrentInning.CurrentRunners.RunnersOnBase.Any())
                        return;
                    string action = await Application.Current?.Windows[0]?.Page?.DisplayActionSheet(ab.ToString(), "Cancel",
                        null, Game.CurrentInning.CurrentRunners.RunnersOnBase.Select(s => s.Runner.FullName).ToArray());
                    if (action != null && action != "Cancel")
                    {
                        var runner = Game.CurrentInning.Runners.Single(s => s.Value.Runner.FullName == action);
                        if (ab == AB.StolenBase)
                        {
                            OnBase nextBase;
                            switch (runner.Key)
                            {
                                case OnBase.First:
                                    nextBase = OnBase.Second;
                                    break;
                                case OnBase.Second:
                                    nextBase = OnBase.Third;
                                    break;
                                default:
                                    nextBase = OnBase.None;
                                    break;
                            }

                            if (runner.Key == OnBase.Third)
                                Game.AddEventToAb(!runner.Value.ReachedOnError ? AB.StealOfHome : AB.StealOfHomeUnearned,
                                    runner.Value, nextBase);
                            else
                                Game.AddEventToAb(AB.StolenBase, runner.Value, nextBase);
                        }
                        else
                        {
                            Game.AddEventToAb(new OutStealing(runner.Value));
                            ScoringIsRequired = true;
                            UpdateRunners();
                        }
                    }
                    add = false;
                    ReplaceCurrentAbInLog();
                    break;
            }
            if (add)
                Game?.UpdateCurrentAbResult(ab, advances);
            OnPropertyChanged(nameof(CurrentAb));
            OnPropertyChanged(nameof(CurrentOuts));
            OnPropertyChanged(nameof(NextBatterText));
            UpdateRunners();
        });
        public ICommand ReturnRunnerCommand => new Command(() =>
        {

        });
        public ICommand PositionLinkCommand => new Command<Position>((pos) =>
        {
            CurrentAb.Result.AddFielder(pos);
            OnPropertyChanged(nameof(CurrentAb));
        });
        public ICommand SelectFromScheduleCommand => new Command(async () =>
        {
            if (_schedule.Count != 0 && GameScores.Count == 0)
            {
                foreach (var game in _schedule.Where(w => w.GameDate > DateTime.Today))
                    GameScores.Add(GameScoreWrapper.Create(game));
            }
            IsSelectingGameFromSchedule = true;
        });
        public ICommand AddPitchCommand => new Command<PitchResult>((pitchType) =>
        {
            var pitch = Pitch.GetPitch(pitchType);
            if (pitchType == PitchResult.InPlay)
                IsPitchesPanelVisible = false;
            Game.AddEventToAb(pitch);
            OnPropertyChanged(nameof(CurrentBalls));
            OnPropertyChanged(nameof(CurrentStrikes));
            ReplaceCurrentAbInLog();
            var pitchNumber = CurrentAb.Pitches.Count();
            UpdatePitches();
            //CurrentAbPitches.Add($"{pitchNumber}) {CurrentAb.Pitches.Last().ToString()}");
        });
        public ICommand CreateGameCommand => new Command(() =>
        {
            var homeTeam = _teams.FirstOrDefault(f => f.Name == SelectedHomeTeam?.Name);
            var awayTeam = _teams.FirstOrDefault(f => f.Name == SelectedAwayTeam?.Name);
            if (homeTeam == null || awayTeam == null)
                return;
            var innings = _leagueDict.ContainsKey(SelectedHomeLeague) ? _leagueDict[SelectedHomeLeague] : 7;
            Game = new BaseballGame(innings);
            Game.SetHomeTeam(homeTeam);
            Game.SetAwayTeam(awayTeam);
            IsConfiguringNewGame = false;
            GameLoaded();
        }, () => SelectedHomeTeam != null && SelectedAwayTeam != null);
        public ICommand CreateGameFromScheduleCommand => new Command(() =>
        {
            var homeLeague = "CMBA";
            var homeTeam = _teams.FirstOrDefault(f => f.Name == SelectedGame?.HomeTeam?.Name);
            if (homeTeam == null)
            {
                var hTeam = _apiTeams.FirstOrDefault(f => f.Name == SelectedGame?.HomeTeam?.Name);

                if (hTeam != null)
                {
                    homeLeague = hTeam.Division;
                    homeTeam = CoreTeam.CreateWithUnknownRoster(hTeam.Name);
                }
            }
            var awayTeam = _teams.FirstOrDefault(f => f.Name == SelectedGame?.AwayTeam?.Name);
            if (awayTeam == null)
            {
                var aTeam = _apiTeams.FirstOrDefault(f => f.Name == SelectedGame?.AwayTeam?.Name);
                if (aTeam != null)
                    awayTeam = CoreTeam.CreateWithUnknownRoster(aTeam.Name);
            }
            if (homeTeam == null || awayTeam == null)
                return;
            var innings = _leagueDict.ContainsKey(homeLeague) ? _leagueDict[homeLeague] : 7;
            Game = new BaseballGame(innings);
            Game.SetHomeTeam(homeTeam);
            Game.SetAwayTeam(awayTeam);
            IsSelectingGameFromSchedule = false;
            GameLoaded();
        });
        public ICommand SetLineupCommand => new Command<bool>((home) =>
        {
            SetLineup = true;
            EditingHomeLineup = home;
            TeamPlayers.Clear();
            if (home)
            {
                UpdatePositionLists(Game.HomeTeam, true);
            }
            else
            {
                UpdatePositionLists(Game.AwayTeam, false);
            }
            UpdatePositionAvailability();

        });
        public ICommand CloseSetLineupCommand => new Command(() =>
        {
            SetLineup = false;
            var team = EditingHomeLineup.GetValueOrDefault() ? Game.HomeTeam : Game.AwayTeam;
            OnPropertyChanged(nameof(team.Lineup));
            OnPropertyChanged(nameof(HomeSubText));
            OnPropertyChanged(nameof(AwaySubText));
            OnPropertyChanged(nameof(Game));
            EditingHomeLineup = null;

        });
        public ICommand AddToLineupCommand => new Command<Player>(async (player) =>
        {
            if (player == null) return;
            var team = EditingHomeLineup.GetValueOrDefault() ? Game?.HomeTeam : Game?.AwayTeam;
            var lineup = EditingHomeLineup.GetValueOrDefault() ? HomeLineup : AwayLineup;
            TeamPlayers.Remove(player);
            var lp = new LineupPosition(player, lineup.Count + 1);
            if (player.Position == Position.P)
            {
                bool confirm = true;
                if (lineup.Any(a => a.Position == Position.P))
                {
                    var currentPitcher = lineup.First(s => s.Position == Position.P);
                    confirm = await Shell.Current.DisplayAlert("Confirm", $"{currentPitcher.Player.FullName} currently selected as pitcher, change to {player.FullName}?", "Confirm", "Cancel");
                    if (!confirm)
                        player.SetPosition(Position.EH);
                }
                if (confirm)
                {
                    team.SetStartingPitcher((Pitcher)player);
                    UpdatePitcherUI(team);
                }
            }
            lineup.Add(lp);
            team?.AddToLineup(player, lp.LineupNumber);
            OnPropertyChanged(nameof(team.Lineup));
            OnPropertyChanged(nameof(team.OrderIsSet));
            UpdatePositionAvailability();
        });
        public ICommand RemoveFromLineupCommand => new Command<LineupPosition>((lp) =>
        {
            if (lp == null) return;
            var team = EditingHomeLineup.GetValueOrDefault() ? Game?.HomeTeam : Game?.AwayTeam;
            var lineup = EditingHomeLineup.GetValueOrDefault() ? HomeLineup : AwayLineup;
            if (team?.RemoveFromLineup(lp.Player) ?? false)
            {
                TeamPlayers.Add(lp.Player);
                lineup.Remove(lp);
                foreach (var spot in lineup)
                    spot.LineupNumber = lineup.IndexOf(spot) + 1;
                UpdatePositionAvailability();
            }
        });
        public ICommand LineupItemDraggedCommand => new Command<LineupPosition>((lp) =>
        {
            _draggedLp = lp;
        });
        public ICommand LineupItemDroppedCommand => new Command<LineupPosition>((lp) =>
        {
            var lineup = EditingHomeLineup.GetValueOrDefault() ? HomeLineup : AwayLineup;
            if (lp == null || _draggedLp == null) return;
            var oldIndex = lineup.IndexOf(_draggedLp);
            var newIndex = lineup.IndexOf(lp);
            lineup.Move(oldIndex, newIndex);
            var val = 1;
            foreach (var position in lineup)
            {
                position.LineupNumber = val;
                val++;
            }
            _draggedLp = null;
        });
        public ICommand HomeSubstitutePlayerCommand => new Command<LineupPosition>(async (replaced) => await SubstitutePlayer(true, replaced));
        public ICommand AwaySubstitutePlayerCommand => new Command<LineupPosition>(async (replaced) => await SubstitutePlayer(false, replaced));
        public ICommand RecordHitCommand => new Command<FieldLocation>(async (loc) =>
        {
            ActiveHitZone = loc;
            await Task.Delay(600);
            if (CurrentAb?.Result is Hit)
                Game?.SetCurrentAbFieldLocation(loc);
            IsFieldOverlayVisible = false;
            ActiveHitZone = null;
        });
        public ICommand SetStatsCommand => new Command<bool>((home) =>
        {
            if (!IsGameStarted && !Game.IsGameOver)
                return;
            ShowStats = true;
            if (home)
            {
                AddStats(GamePitchingStats, Game.HomeTeamPitching);
                AddStats(GameHittingStats, Game.HomeTeamHitting);
            }
            else
            {
                AddStats(GamePitchingStats, Game.AwayTeamPitching);
                AddStats(GameHittingStats, Game.AwayTeamHitting);
            }
        });
        public ICommand CloseStatsViewCommand => new Command(() => { ShowStats = false; });
        public ICommand CloseLineScoreCommand => new Command(() => { ShowLineScore = false; });
        public ICommand CloseGameSelectionCommand => new Command(() => IsSelectingGameFromSchedule = IsConfiguringNewGame = false);
        public bool SetLineup
        {
            get => _setLineup;
            set
            {
                _setLineup = value;
                OnPropertyChanged(nameof(SetLineup));
            }
        }
        private void AddStats<T>(ObservableCollection<StatsRow<T>> collection, List<T> stats) where T : IHasPlayer
        {
            collection.Clear();
            Color oddColor = Colors.White;
            Color evenColor = Colors.LightGray;
            for (int i = 0; i < stats.Count; i++)
            {
                var backColor = i % 2 == 0 ? evenColor : oddColor;
                collection.Add(new StatsRow<T>(stats[i], backColor));
            }
        }
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
        private PStats GetCurrentPitcherStats()
        {
            var stats = Game.CurrentInning.Half == HalfInning.Top ? _game.HomeTeamPitching : Game.AwayTeamPitching;
            return stats.First(s => s.Player == Game.CurrentInning.CurrentPitcher);
        }
        public async Task LoadTeamsAndLeagues()
        {
            using (var client = new HttpClient() { BaseAddress = new Uri("https://h503cfkn-7249.usw3.devtunnels.ms/") })
            {
                var teams = await client.GetFromJsonAsync<List<Team>>("/api/teams");
                foreach (var team in teams)
                    _apiTeams.Add(team);
                foreach (var league in teams.Select(t => t.Division).Distinct())
                    Leagues.Add(league);
            }
        }
        public async Task LoadSchedule(int teamId)
        {
            using (var client = new HttpClient() { BaseAddress = new Uri("https://h503cfkn-7249.usw3.devtunnels.ms/") })
            {
                var games = await client.GetFromJsonAsync<List<GameScore>>($"/api/teams/{teamId}/games");
                foreach (var game in games)
                    _schedule.Add(game);
            }
        }
        private async Task SubstitutePlayer(bool home, LineupPosition lp)
        {

            if (lp is null)
                return;
            var replaced = lp.Player;
            var team = home ? Game?.HomeTeam : Game?.AwayTeam;
            var lineup = home ? HomeLineup : AwayLineup;
            string action = await Application.Current.Windows[0].Page?.DisplayActionSheet($"Sub for {replaced.DisplayName}", "Cancel", null, [.. team.Bench.Select(s => s.FullName)]);
            if (action != "Cancel" && !string.IsNullOrEmpty(action))
            {
                var newPlayer = team.Roster.FirstOrDefault(s => s.FullName == action);
                int index = team.Lineup.IndexOf(replaced);
                if (index != -1 && newPlayer != null)
                {
                    var diagResult = await Shell.Current.DisplayAlert("Confirm", $"Substitute {newPlayer.LastName} for {replaced.LastName}?", "Yes", "No");
                    if (!diagResult)
                        return;
                    var sub = Game.Substitute(team, newPlayer, replaced);
                    lineup.RemoveAt(index);
                    lineup.Insert(index, new LineupPosition(newPlayer, index + 1));
                    Game.AddEventToAb(sub);
                    UpdatePositionAvailability(); // Re-run your strikethrough logic
                    OnPropertyChanged(nameof(Game));
                    UpdateRunners();
                }
            }
        }
        internal void FilterTeams(bool? home = null)
        {
            if (!home.HasValue || home.Value)
            {
                FilteredHomeTeams.Clear();
                if (SelectedHomeLeague != null)
                {
                    foreach (var team in _apiTeams.Where(w => w.Division == SelectedHomeLeague))
                        FilteredHomeTeams.Add(team);
                }
                else
                {
                    foreach (var team in _apiTeams)
                        FilteredHomeTeams.Add(team);
                }
            }
            if (!home.HasValue || !home.Value)
            {
                FilteredAwayTeams.Clear();
                if (SelectedAwayLeague != null)
                {
                    foreach (var team in _apiTeams.Where(w => w.Division == SelectedAwayLeague))
                        FilteredAwayTeams.Add(team);
                }
                else
                {
                    foreach (var team in _apiTeams)
                        FilteredAwayTeams.Add(team);
                }
            }
        }
        internal void LoadTeamsFromXml()
        {
            var scoreBookPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Scorebook");
            var rosterPath = Path.Combine(scoreBookPath, "Rosters.xml");
            if (!File.Exists(rosterPath))
                return;
            var leagues = League.CreateFromFile(rosterPath, out League current);
            if (leagues.Any())
                _teams = [.. leagues.SelectMany(s => s.Teams)];

        }
        public Dictionary<Position, bool> PositionOccupiedMap { get; set; } = [];
        internal void UpdatePositionAvailability()
        {
            foreach (var pos in Position.All)
                PositionOccupiedMap[pos] = false;
            foreach (var lp in ActiveLineup)
            {
                lp.IsConflict = lp.Position == Position.EH ? false : PositionOccupiedMap[lp.Position];
                PositionOccupiedMap[lp.Position] = true;
                if (lp.HasDH)
                {
                    lp.IsConflict = lp.HittingFor.Position == Position.EH ? false : PositionOccupiedMap[lp.HittingFor.Position];
                    PositionOccupiedMap[lp.HittingFor.Position] = true;
                }
            }
            var counts = ActiveLineup.Where(p => p.Position != null).GroupBy(p => p.Position).ToDictionary(g => g.Key, g => g.Count());
            var dhPositions = ActiveLineup.Select(s => s.HittingFor).Where(w => w is not null).Select(s => s.Position);
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
            OnPropertyChanged(nameof(PositionStatusList));
            OnPropertyChanged(nameof(Defense));
        }

        private BaseballGame? _game;
        private AtBat? _currentAb;
        private bool _isPitchesPanelVisible;
        private bool _isConfiguringNewGame;
        private bool _isSelectingGameFromSchedule;
        private string _selectedHomeLeague;
        private string _selectedAwayLeague;
        private Team? _selectedHomeTeam;
        private Team? _selectedAwayTeam;
        private bool _setLineup;
        private bool? _editingHomeLineup;
        private bool _isGameStarted;
        private int _currentRbis;
        private bool _scoringIsRequired;
        private bool _homePitcherSelected;
        private bool _awayPitcherSelected;
        private bool _runnerOnFirstIsOut;
        private bool _runnerOnSecondIsOut;
        private bool _runnerOnThirdIsOut;
        private bool _sendRunnerBackButtonVisible;
        private bool _showDefensiveAlignment;
        private bool _isFieldOverlayVisible;
        private bool _saveAwarded;
        private bool _isSideBarOpen;
        private string _saveAwardedTo = "";
        private LineupPosition? _draggedLp;
        private FieldLocation? _activeHitZone;
        private PitchTotals? _currentPitchStats;
        private bool _showStats;
        private bool _gameIsOver;
        private int _totalInningCount;
        private bool _showLineScore;
        private readonly Dictionary<string, int> _leagueDict = new()
        {
            { "CMBA", 7 },
            { "BJL", 9 },
            { "CSYBL", 7 }
        };
        private List<CoreTeam> _teams = [];
        private List<Team> _apiTeams = [];
        private List<GameScore> _schedule = [];
    }
}
