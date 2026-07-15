using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using Scorebook.ViewObjects;
using System.Collections.ObjectModel;
using System.Text;

namespace Scorebook.Coordinators
{
    public class GameCoordinator
    {
        public GameCoordinator()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                _runnerMenuOptions.Add("Stolen Base");
                _runnerMenuOptions.Add("Out Stealing");
            }
        }
        public ScorebookViewModel? ViewModel { get; set; }
        internal void GameEnded(object? sender, EventArgs e)
        {
            ViewModel.GameIsOver = true;
            ViewModel.LineScore.Clear();
            ViewModel.Game.SetGameEndTime(ViewModel.GameSelection?.EndDateTime);
            UpdateLineScore();
            ViewModel.OnPropertyChanged(nameof(ViewModel.Game));
            ViewModel.ShowLineScore = true;
            ViewModel.InningNumber = ScorebookViewModel.FinalText;
            ViewModel.IsTopHalfOfInning = false;
            ViewModel.IsBottomHalfOfInning = false;
            ViewModel.GameManager.SetGameEnded(ViewModel.Game.EndTime);
            ViewModel.GameSelection.OnPropertyChanged(nameof(ViewModel.GameSelection.GameInProgress));
        }
        internal void UpdateLineScore()
        {
            ViewModel.TotalInningCount = new int[] { 7, ViewModel.Game.Innings.Max(m => m.Number) }.Max();
            var grps = ViewModel.Game?.Innings.GroupBy(g => g.Number);
            for (int i = 1; i <= ViewModel.TotalInningCount; i++)
            {
                var grp = grps?.SingleOrDefault(s => s.Key == i);
                if (grp != null)
                    ViewModel.LineScore.Add(new LineScoreData(i, grp));
                else
                    ViewModel.LineScore.Add(new LineScoreData(i));
            }
            if (ViewModel.Game?.SaveAwardedTo is not null)
            {
                ViewModel.SaveAwarded = true;
                ViewModel.SaveAwardedTo = ViewModel.Game.SaveAwardedTo.FullName;
            }
        }
        internal async void InningEnded(object? sender, InningChangeEventArgs e)
        {
            ViewModel.InningEvents.Clear();
            var stats = GetCurrentPitcherStats();
            ViewModel.CurrentPitchStats = new PitchTotals(stats);
            await ViewModel.GameManager.UpdateInning(ViewModel.Game.CurrentInning);
        }
        internal async void InningStarted(object? sender, InningChangeEventArgs e)
        {
            ViewModel.InningNumber = ViewModel.Game.CurrentInning.Number.ToString();
            ViewModel.IsTopHalfOfInning = ViewModel.Game.CurrentInning.Half == HalfInning.Top;
            ViewModel.IsBottomHalfOfInning = ViewModel.Game.CurrentInning.Half == HalfInning.Bottom;
            if (ViewModel.Game.CurrentInning.Half == HalfInning.Top)
            {
                ViewModel.HomeTeam.Defense.RefreshPositions(ViewModel.HomeTeam.Lineup);
                ViewModel.Defense = ViewModel.HomeTeam.Defense;
            }
            else
            {
                ViewModel.AwayTeam.Defense.RefreshPositions(ViewModel.AwayTeam.Lineup);
                ViewModel.Defense = ViewModel.AwayTeam.Defense;
            }
            await ViewModel.GameManager.SetNextInning();
        }
        internal void InningUpdated(object? sender, EventArgs e)
        {
            var inning = ViewModel.Game.CurrentInning;
            ViewModel.GameManager?.UpdateAb(inning.CurrentAb);
            ViewModel.GameManager?.UpdateInning(inning);
        }
        internal void ScoreChanged(object? sender, ScoreChangedEventArgs e)
        {
            ViewModel.OnPropertyChanged(nameof(ViewModel.Game.HomeScore));
            ViewModel.OnPropertyChanged(nameof(ViewModel.Game.AwayScore));
            ViewModel.GameManager.UpdateScore(ViewModel.Game.HomeScore, ViewModel.Game.AwayScore);
        }
        internal void ScoringUpdated(object? sender, EventArgs e)
        {
            ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentAb));
        }
        internal PStats GetCurrentPitcherStats()
        {
            var stats = ViewModel.Game.CurrentInning.Half == HalfInning.Top ? ViewModel.Game.HomeTeamPitching : ViewModel.Game.AwayTeamPitching;
            return stats.FirstOrDefault(s => s.Player == ViewModel.Game.CurrentInning.CurrentPitcher);
        }
        internal async Task ScoringEntered(AB ab, BaseballGame? Game)
        {
            if (Game is null || await ShowNextBatterWarning())
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
                    ViewModel.IsFieldOverlayVisible = true;
                    break;
                case AB.Double:
                    advances = await HandleRunnerAdvances(2);
                    ViewModel.IsFieldOverlayVisible = true;
                    break;
                case AB.Triple:
                    advances = await HandleRunnerAdvances(3);
                    ViewModel.IsFieldOverlayVisible = true;
                    break;
                case AB.HomeRun:
                    advances = await HandleRunnerAdvances(4);
                    ViewModel.IsFieldOverlayVisible = true;
                    break;
                case AB.StrikeOut:
                    var lastPitch = ViewModel.CurrentAb?.Pitches?.LastOrDefault();
                    if (lastPitch is not null)
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
                        chargeEarned = await ShowEarnedDialog(Game.CurrentInning.CurrentRunners.OnThird.Runner);
                    advances = Game.ForceRunners(true, chargeEarned);
                    break;
                case AB.FieldersChoice:
                    ViewModel.ScoringIsRequired = true;
                    break;
                case AB.Sacrifice:
                    advances = Game.AdvanceAllRunners(1, false);
                    break;
                case AB.StolenBase:
                case AB.OutStealing:
                    if (!Game.CurrentInning.CurrentRunners.RunnersOnBase.Any())
                        return;
                    string action = await Application.Current.MainPage?.DisplayActionSheet(ab.ToString(), "Cancel",
                        null, [.. Game.CurrentInning.CurrentRunners.RunnersOnBase.Select(s => s.Runner.FullName)]);
                    if (action != null && action != "Cancel")
                    {
                        var runner = Game.CurrentInning.Runners.Single(s => s.Value.Runner.FullName == action);
                        if (ab == AB.StolenBase)
                        {
                            var nextBase = runner.Key switch
                            {
                                OnBase.First => OnBase.Second,
                                OnBase.Second => OnBase.Third,
                                _ => OnBase.None,
                            };
                            if (runner.Key == OnBase.Third)
                                Game.AddEventToAb(!runner.Value.ReachedOnError ? AB.StealOfHome : AB.StealOfHomeUnearned,
                                    runner.Value, nextBase);
                            else
                                Game.AddEventToAb(AB.StolenBase, runner.Value, nextBase);
                        }
                        else
                        {
                            Game.AddEventToAb(new OutStealing(runner.Value));
                            ViewModel.ScoringIsRequired = true;
                            ViewModel.UpdateRunners();
                        }
                    }
                    add = false;
                    ViewModel.ReplaceCurrentAbInLog();
                    break;
            }
            if (add)
                Game?.UpdateCurrentAbResult(ab, advances);
            var rbis = advances?.OfType<RunScored>()?.Count() ?? 0;
            ViewModel.CurrentRbis = rbis;
            ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentAb));
            ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentOuts));
            ViewModel.OnPropertyChanged(nameof(ViewModel.NextBatterText));
            ViewModel.UpdateRunners();
            if (ViewModel.MobileActionTrigger)
                ViewModel.ShowActionDialog = false;
        }
        internal async Task ShowOtherMenu(BaseballGame? game)
        {
            if (game is null)
                return;
            IList<RunningEvent>? advances = null;
            string action = await Application.Current?.Windows[0]?.Page?.DisplayActionSheet("Options", "Cancel",
                        null, "Balk", "Wild Pitch", "Passed Ball", "Sacrifice Fly", "Sac / Reached on Error", "Drop Third Strike");
            switch (action)
            {
                case "Balk":
                    game?.AddEventToAb(AB.Balk, (Player)game.CurrentAb.Pitcher, game.AdvanceAllRunners(1, false));
                    break;
                case "Wild Pitch":
                case "Passed Ball":
                    var ab = action == "Wild Pitch" ? AB.WildPitch : AB.PassedBall;
                    advances = game.AdvanceAllRunners(1, false);
                    game.AddRunnerAdvances(ab, advances);
                    if (advances.Any(a => a is RunScored))
                        ViewModel.SendRunnerBackButtonVisible = true;
                    ViewModel.ReplaceCurrentAbInLog();
                    break;
                case "Sacrifice Fly":
                    await UpdateCurrentAbResult(AB.SacrificeFly, game.AdvanceAllRunners(1, false));
                    break;
                case "Sac / Reached on Error":
                    await UpdateCurrentAbResult(AB.SacrificeReachOnError, game.AdvanceAllRunners(1, true));
                    break;
                case "Drop Third Strike":
                    await UpdateCurrentAbResult(AB.DropThirdStrike, game.ForceRunners());
                    break;
            }
            ViewModel.UpdateRunners();
            ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentAb));
        }
        private async Task<IList<RunningEvent>> HandleRunnerAdvances(int bases)
        {
            var advances = ViewModel.Game.AdvanceAllRunners(bases);
            if (advances.Any(a => a is RunScored))
            {
                advances = await CheckRunsScoredForEarned(advances);
            }
            return advances;
        }
        internal async Task AdvanceRunners(OnBase onBase, AdvanceReason reason)
        {
            if (ViewModel?.Game is null || ViewModel.Game.CurrentInning?.CurrentRunners is null)
                return;
            var runners = ViewModel.Game.CurrentInning.CurrentRunners;
            switch (onBase)
            {
                case OnBase.Third:
                    {
                        var chargeAsEarned = true;
                        if (ViewModel.Game?.CurrentInning.Errors > 0 || ViewModel.Game.CurrentAb.Result.Errors > 0)
                            chargeAsEarned = await ShowEarnedDialog(runners.OnThird.Runner);
                        ViewModel.Game.AddEventToAb(ViewModel.Game.ScoreRunner(runners.OnThird, reason, chargeAsEarned));
                        break;
                    }
                case OnBase.Second:
                    ViewModel.Game.AddEventToAb(ViewModel.Game.AdvanceRunner(runners.OnSecond, OnBase.Third, reason));
                    break;
                case OnBase.First:
                    ViewModel.Game.AddEventToAb(ViewModel.Game.AdvanceRunner(runners.OnFirst, OnBase.Second, reason));
                    break;
            }

            ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentAb));
            ViewModel.UpdateRunners();
        }
        internal async Task NextBatter(BaseballGame? game)
        {
            try
            {
                if (!ViewModel.IsGameStarted)
                {
                    if (game?.HomeTeam is null || game.AwayTeam is null)
                    {
                        await Shell.Current.DisplayAlert("Error", "Select teams before starting game", "OK");
                        return;
                    }
                    if (ViewModel.GameSelection.SelectedGame is not null)
                        ViewModel.GameManager.SetStartDateTime(ViewModel.GameSelection.StartDateTime);
                    game?.StartGame(ViewModel.GameSelection.SelectedGame?.GameDate ?? DateTime.Today, ViewModel.GameSelection?.StartDateTime ?? DateTime.Now);
                    ViewModel.IsGameStarted = true;
                    ViewModel.OnPropertyChanged(nameof(ViewModel.Game));
                    ViewModel.OnPropertyChanged(nameof(ViewModel.Defense));
                    if (!ViewModel.HomeTeam?.Lineup.Any() ?? false)
                        ViewModel.HomeTeam?.FillLineup(false);
                    if (!ViewModel.AwayTeam?.Lineup.Any() ?? false)
                        ViewModel.AwayTeam?.FillLineup(false);
                    await ViewModel.LinkAb();
                }
                else
                {
                    ViewModel.SendRunnerBackButtonVisible = false;
                    ViewModel.IsFieldOverlayVisible = false;
                    if (ViewModel.ScoringIsRequired)
                    {
                        var scoringAdded = game?.AddScoring();
                        ViewModel.ScoringIsRequired = false;
                        if (!(ViewModel.CurrentAb?.IsFinished ?? false) && ViewModel.CurrentAb?.Result is FieldersChoice fc)
                        {
                            var advances = game?.ForceRunners();
                            fc.AddAdvances(advances);
                            ViewModel.UpdateRunners();
                        }
                    }
                    else if (!game?.FinishAb() ?? false)
                    {
                        await Shell.Current.DisplayAlert("Error", "Add scoring before moving to next batter", "OK");
                        return;
                    }

                    if (!game?.IsGameOver ?? false)
                        await ViewModel.LinkAb();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
        internal async void AddPitch(PitchResult pitchType)
        {
            var pitch = Pitch.GetPitch(pitchType);
            ViewModel.Game?.AddEventToAb(pitch);
            switch (pitchType)
            {
                case PitchResult.InPlay:
                    ViewModel.IsPitchesPanelVisible = false;
                    break;
                case PitchResult.Ball:
                    if (ViewModel.CurrentBalls == 4)
                        ViewModel.IsPitchesPanelVisible = false;
                    break;
                case PitchResult.SwingingStrike:
                case PitchResult.CalledStrike:
                    if (ViewModel.CurrentStrikes >= 3)
                        ViewModel.IsPitchesPanelVisible = false;
                    break;
                default:
                    break;
            }
            ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentBalls));
            ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentStrikes));
            ViewModel.ReplaceCurrentAbInLog();
            ViewModel.UpdatePitches();
            await ViewModel.GameManager.UpdateAb(ViewModel?.CurrentAb);
        }
        internal void SetStats(bool home)
        {
            if (!ViewModel.IsGameStarted && !ViewModel.GameIsOver)
                return;
            ViewModel.ShowStats = true;
            if (home)
            {
                AddStats(ViewModel.GamePitchingStats, ViewModel.Game?.HomeTeamPitching);
                AddStats(ViewModel.GameHittingStats, ViewModel.Game?.HomeTeamHitting);
            }
            else
            {
                AddStats(ViewModel.GamePitchingStats, ViewModel.Game?.AwayTeamPitching);
                AddStats(ViewModel.GameHittingStats, ViewModel.Game.AwayTeamHitting);
            }
        }
        internal async Task HandleRunningEvents(OnBase onBase)
        {
            string header = "";
            var runners = ViewModel.Game?.CurrentInning.CurrentRunners;
            switch (onBase)
            {
                case OnBase.First:
                    if (!runners?.RunnerOnFirst ?? false)
                        return;
                    header = "Runner On First";
                    break;
                case OnBase.Second:
                    if (!runners?.RunnerOnSecond ?? false)
                        return;
                    header = "Runner On Second";
                    break;
                case OnBase.Third:
                    if (!runners?.RunnerOnThird ?? false)
                        return;
                    header = "Runner On Third";
                    break;
            }
            string action = await Application.Current.MainPage?.DisplayActionSheet(header, "Cancel", null, _runnerMenuOptions.ToArray());
            if (action == _runnerMenuOptions[0])  // "Advance Runner"
                await AdvanceRunners(onBase, AdvanceReason.Ab);
            else if (action == _runnerMenuOptions[1])  // "Runner Advanced On Throw"
                await AdvanceRunners(onBase, AdvanceReason.Throw);
            else if (action == _runnerMenuOptions[2]) // Move Runner Back
                ViewModel.Game?.ReturnRunner(onBase);
            else if (action == _runnerMenuOptions[3]) // Runner Out At Base
            {
                ViewModel.ScoringIsRequired = true;
                AddRunnerOutEvent(onBase, false);
            }
            else if (action == _runnerMenuOptions[4]) // Runner Advanced On Error
            {
                ViewModel.ScoringIsRequired = true;
                await ViewModel.GameCoordinator.AdvanceRunners(onBase, AdvanceReason.Error);
            }
            else if (action == _runnerMenuOptions[5]) // Runner Out Advancing
            {
                if (!ViewModel.CurrentAb?.Result.HasFielders ?? false)
                    ViewModel.ScoringIsRequired = true;
                AddRunnerOutEvent(onBase, true);
            }
            else if (action == _runnerMenuOptions[6]) // Courtesy Runner
            {
                var bench = ViewModel.Game?.BattingTeam.Roster;
                var allRunners = bench?.Select(s => s.FullName).ToArray();
                var cr = await Application.Current.MainPage?.DisplayActionSheet("Select Courtesy Runner", "Cancel", null, allRunners);
                if (cr != null && cr != "Cancel")
                {
                    var nameNumber = cr.Split('-');
                    var runner = ViewModel.Game?.BattingTeam.GetPlayer(nameNumber[1].Split(',')[0], int.Parse(nameNumber[0]));
                    var previousRunner = ViewModel.Game?.CurrentInning.CurrentRunners[onBase];
                    ViewModel.Game?.CurrentInning.AddCourtesyRunner(runner, previousRunner);
                }
            }
            else if (action == _runnerMenuOptions[7]) // Stolen Base
            {
                var runner = ViewModel.Game?.CurrentInning.CurrentRunners[onBase];
                var nextBase =onBase switch
                {
                    OnBase.First => OnBase.Second,
                    OnBase.Second => OnBase.Third,
                    _ => OnBase.None,
                };
                if (onBase == OnBase.Third)
                    ViewModel?.Game.AddEventToAb(!runner.ReachedOnError ? AB.StealOfHome : AB.StealOfHomeUnearned, runner, nextBase);
                else
                    ViewModel?.Game.AddEventToAb(AB.StolenBase, runner, nextBase);

            }
            else if (action == _runnerMenuOptions[8]) // Out Stealing
            {
                var runner = ViewModel?.Game.CurrentInning.CurrentRunners[onBase];
                ViewModel?.Game.AddEventToAb(new OutStealing(runner));
                ViewModel.ScoringIsRequired = true;
                ViewModel.UpdateRunners();
            }
            ViewModel.ReplaceCurrentAbInLog();
            ViewModel.UpdateRunners();
        }
        private void AddRunnerOutEvent(OnBase onBase, bool advance)
        {
            var runners = ViewModel.Game?.CurrentInning.CurrentRunners;
            if (onBase == OnBase.Third)
                ViewModel.Game?.AddRunnerOutEvent(runners?.OnThird, advance ? OnBase.None : OnBase.Third);
            if (onBase == OnBase.Second)
                ViewModel.Game?.AddRunnerOutEvent(runners?.OnSecond, advance ? OnBase.Third : OnBase.Second);
            if (onBase == OnBase.First)
                ViewModel.Game?.AddRunnerOutEvent(runners?.OnFirst, advance ? OnBase.Second : OnBase.First);
            ViewModel.UpdateRunners();
        }
        private async Task UpdateCurrentAbResult(AB ab, IList<RunningEvent> advances)
        {
            if (await ShowNextBatterWarning())
                return;
            ViewModel.Game?.UpdateCurrentAbResult(ab, advances);
        }
        private static void AddStats<T>(ObservableCollection<StatsRow<T>> collection, List<T> stats) where T : IHasPlayer
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
        private async Task<IList<RunningEvent>> CheckRunsScoredForEarned(IList<RunningEvent> advances)
        {
            if (ViewModel.Game?.CurrentInning.Errors > 0)
            {
                for (int i = 0; i < advances.Count; i++)
                {
                    var advance = advances[i];
                    var runner = ViewModel.Game.CurrentInning.CurrentRunners.RunnersOnBase.SingleOrDefault(s => s.Runner == advance.Player);
                    if (runner is not null && advance is RunScored && !runner.ReachedOnError)
                    {
                        var result = await ShowEarnedDialog(advance.Player);
                        if (!result)
                            advances[i] = RunScored.Unearned(advance);
                    }
                }
            }
            return advances;
        }
        private async Task<bool> ShowNextBatterWarning()
        {
            if (ViewModel.CurrentAb?.Result?.FinishedAb ?? false)
            {
                await Shell.Current.DisplayAlert("Warning", "Current AB is scored, undo to change.", "Ok");
                return true;
            }
            return false;
        }
        private static async Task<bool> ShowEarnedDialog(Player runner)
        {
            return await Shell.Current.DisplayAlert("Earned Run", $"Errors occurred this inning, charge {runner.DisplayName}'s run as earned?", "Yes", "No");
        }
        internal static async Task SaveGame(BaseballGame game)
        {
            if (game is null)
                return;
            string fileName = $"{game.HomeTeam.Name}{game.AwayTeam.Name}{game.GameDate:Md}.sbg";
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                string localPath = Path.Combine(ScorebookViewModel.LocalSavePath, fileName);
                game.SaveAs(localPath);
                await Application.Current.MainPage?.DisplayAlert("Saved", "Game progress saved to device.", "OK");
            }
            else
            {
                string mainDir = FileSystem.CacheDirectory;
                var filePath = Path.Combine(mainDir, fileName);
                game.SaveAs(filePath);
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Upload Game To Cloud",
                    File = new ShareFile(filePath, "text/plain")
                });
            }
        }

        private readonly List<string> _runnerMenuOptions = [
            "Advance Runner",
            "Runner Advanced On Throw",
            "Move Runner Back",
            "Runner Out At Base",
            "Runner Advanced On Error",
            "Runner Out Advancing",
            "Courtesy Runner"];
    }
}