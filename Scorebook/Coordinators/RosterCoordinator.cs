using Electrons.Core.Net8.Games;
using Scorebook.Messages;
using Scorebook.ViewObjects;


namespace Scorebook.Coordinators
{
    public class RosterCoordinator
    {
        public RosterCoordinator() { }
        public ScorebookViewModel ViewModel { get; set; }
        public async Task SelectPitcher(bool home)
        {
            var team = home ? ViewModel.HomeTeam : ViewModel.AwayTeam;
            var allPitchers = team.CoreTeam.AvailablePitchers.Select(s => s.FullName).OrderBy(o => o).ToArray();
            var pitcher = await Application.Current.MainPage.DisplayActionSheet("Select Pitcher", "Cancel", null, allPitchers);
            var selectedPitcher = team.CoreTeam.AvailablePitchers.SingleOrDefault(s => s.FullName == pitcher);
            if (selectedPitcher is null || selectedPitcher == (Player)team.CoreTeam.CurrentPitcher)
                return;
            if (team.CoreTeam.CurrentPitcher is null || !team.CoreTeam.OrderIsSet || !ViewModel.Game.IsStarted || (ViewModel.Game.IsStarted && team.CanReplacePitcher))
            {
                team.CoreTeam.SetStartingPitcher((Pitcher)selectedPitcher);
                ViewModel.CurrentPitchStats = new PitchTotals(selectedPitcher.DisplayName);
                if (ViewModel.Game.IsStarted && selectedPitcher.IsMemberOf(ViewModel.Game.FieldingTeam))
                    ViewModel.Game.CurrentInning.SetCurrentPitcher((Pitcher)selectedPitcher);
                team.PitcherSelected = true;
                team.CanReplacePitcher = false;
                return;
            }
            var result = await Shell.Current.DisplayAlert("Confirm", $"Replace {team.CoreTeam.CurrentPitcher.LastName} with {selectedPitcher.LastName}?  ", "Yes", "No");
            if (!result)
                return;
            var sub = ViewModel.Game.ChangePitcher(team.CoreTeam, selectedPitcher);
            ViewModel.Game.AddEventToAb(sub);
            ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentAb));
            ViewModel.UpdatePitches();
        }
        internal async Task AddToLineup(TeamWrapper team, Player player)
        {
            if (player == null) return;
            team.TeamPlayers.Remove(player);
            team.Bench.Remove(player);
            if (player.Position == Position.P)
            {
                bool confirm = true;
                if (team.Lineup.Any(a => a.Position == Position.P))
                {
                    var currentPitcher = team.Lineup.First(s => s.Position == Position.P);
                    confirm = await Shell.Current.DisplayAlert("Confirm", $"{currentPitcher.Player.FullName} currently selected as pitcher, change to {player.FullName}?", "Confirm", "Cancel");
                    if (!confirm)
                        player.SetPosition(Position.EH);
                }
                if (confirm)
                {
                    team.CoreTeam.SetStartingPitcher((Pitcher)player);
                    UpdatePitcherUI(team);
                }
            }
            else if (player.Position == Position.DH)
            {
                player.SetPosition(Position.EH);
            }
            var lp = new LineupPosition(player, team.Lineup.Count + 1);
            team.Lineup.Add(lp);
            team.CoreTeam.AddToLineup(player, lp.LineupNumber);
            ViewModel.OnPropertyChanged(nameof(team.Lineup));
            ViewModel.OnPropertyChanged(nameof(team.CoreTeam.OrderIsSet));
            ViewModel.OnPropertyChanged(nameof(team.Bench));
            team.UpdatePositionAvailability();
            team.UpdateLineup(lp, player);
        }
        internal void CloseSetLineup(TeamWrapper team)
        {
            ViewModel.OnPropertyChanged(nameof(team.Lineup));
            ViewModel.OnPropertyChanged(nameof(team.SubText));
            ViewModel.OnPropertyChanged(nameof(ViewModel.Game));
            team.IsEditing = false;
        }
        internal async void HandlePositionChangedMesage(object recipient, PositionChangedMessage message)
        {
            if (message.Value != null)
            {
                var test = recipient as TeamWrapper;
                var team = ViewModel.HomeTeam.IsEditing ? ViewModel.HomeTeam : ViewModel.AwayTeam;
                if (message.Value.Equals(Position.DH))
                {
                    var dhPlayerName = await Application.Current?.Windows[0]?.Page?.DisplayActionSheet("Select player to  DH for", "Cancel", null, team.CoreTeam.Bench.Select(s => s.FullName).ToArray());
                    var dhPositionText = await Application.Current?.Windows[0]?.Page?.DisplayActionSheet("Select defensive position", "Cancel", null, [.. Position.All.Select(s => s.LongPositionString)]);
                    if (dhPlayerName is not null && dhPlayerName != "Cancel" && dhPositionText != "Cancel")
                    {
                        var player = team.Lineup.FirstOrDefault(f => f.Position == Position.DH);
                        var dhPlayer = team.CoreTeam.Roster.Single(s => s.FullName == dhPlayerName);
                        var dhPosition = Position.All.Single(s => s.LongPositionString == dhPositionText);
                        if (dhPosition == Position.P && !team.CoreTeam.OrderIsSet)
                        {
                            team.CoreTeam.SetStartingPitcher((Pitcher)dhPlayer);
                            UpdatePitcherUI(team);
                        }
                        dhPlayer.SetPosition(dhPosition);
                        player.HittingFor = dhPlayer;
                    }
                }
                if (message.Value.Equals(Position.P))
                {
                    var lp = team.Lineup.FirstOrDefault(f => f.Position == Position.P);
                    if (lp != null && !team.CoreTeam.OrderIsSet)
                    {
                        team.CoreTeam.SetStartingPitcher((Pitcher)lp.Player);
                        UpdatePitcherUI(team);
                    }
                }
                team.UpdatePositionAvailability();
            }
        }
        internal void LineupItemDropped(TeamWrapper team, LineupPosition lp)
        {
            var lineup = team.Lineup;
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
            team.CoreTeam.SetBattingOrder(lineup.Select(s => s.Player).ToList());
            _draggedLp = null;
        }
        internal async Task RemoveFromLineup(TeamWrapper team, LineupPosition lp)
        {
            if (lp == null || team is null) return;
            if (!team.CoreTeam.RemoveFromLineup(lp.Player))
            {
                var result = await Application.Current.MainPage.DisplayAlert("Confirm", "Game is started, remove anyway?", "ok", "cancel");
                if (!result) return;
            }
            if (!team.CoreTeam.RemoveFromLineup(lp.Player, true))
                return;
            team.TeamPlayers.Add(lp.Player);
            team.Lineup.Remove(lp);
            foreach (var spot in team.Lineup)
                spot.LineupNumber = team.Lineup.IndexOf(spot) + 1;
            team.UpdatePositionAvailability();
        }
        internal void UpdatePitcherUI(TeamWrapper team)
        {
            if (team.CoreTeam == ViewModel.Game?.HomeTeam)
                ViewModel.HomeTeam.PitcherSelected = true;
            else
                ViewModel.AwayTeam.PitcherSelected = true;
        }
        internal void SetDraggedLineupPosition(LineupPosition lp)
        {
            _draggedLp = lp;
        }
        internal async Task SubstitutePlayer(bool home, LineupPosition lp)
        {
            try
            {
                if (lp is null)
                    return;
                var replaced = lp.Player;
                var team = home ? ViewModel.HomeTeam : ViewModel.AwayTeam;
                string action;
                if (lp.CanReplace || team.IsUnknownRoster)
                {
                    var headerText = lp.CanReplace ? "Unknown Player" : $"Sub for {replaced.DisplayName}";
                    action = await Application.Current.MainPage?.DisplayPromptAsync(headerText, "Enter player Number", "Ok", "Cancel", maxLength: 2, keyboard: Keyboard.Numeric);
                }
                else
                    action = await Application.Current.MainPage?.DisplayActionSheet($"Sub for {replaced.DisplayName}", "Cancel", null, [.. team.CoreTeam.Roster.Select(s => s.FullName)]);
                if (action != "Cancel" && !string.IsNullOrEmpty(action))
                {
                    Player newPlayer;
                    if (lp.CanReplace)
                    {
                        newPlayer = GetPlayer(team, action);
                        team.CoreTeam.ReplaceUnknown(lp.LineupNumber, newPlayer);
                        lp.Player = newPlayer;
                        ViewModel.Game.UpdatePlayer(replaced, newPlayer);
                        ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentAb));
                    }
                    else
                    {
                        newPlayer = GetPlayer(team, action);
                        var diagResult = await Shell.Current.DisplayAlert("Confirm", $"Substitute {newPlayer.DisplayName} for {replaced.DisplayName}?", "Yes", "No");
                        if (!diagResult)
                            return;
                        var sub = ViewModel.Game.Substitute(team.CoreTeam, newPlayer, replaced);
                        team.UpdateLineup(lp, newPlayer);
                        team.Bench.Remove(newPlayer);
                        team.Replaced.Add(replaced);
                        ViewModel.Game.AddEventToAb(sub);
                        ViewModel.ReplaceCurrentAbInLog();
                        ViewModel.CurrentAb = ViewModel.Game.CurrentAb;
                        ViewModel.InningEvents.Insert(0, ViewModel.CurrentAb.ToString());
                        ViewModel.LinkAb();
                        ViewModel.OnPropertyChanged(nameof(ViewModel.CurrentAb));
                    }
                    team.UpdatePositionAvailability(); // Re-run your strikethrough logic
                    ViewModel.OnPropertyChanged(nameof(ViewModel.Game));
                    ViewModel.OnPropertyChanged(nameof(team.Replaced));
                    ViewModel.OnPropertyChanged(nameof(team.Bench));
                    ViewModel.UpdateRunners();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private Player GetPlayer(TeamWrapper team, string action)
        {
            var newPlayer = team.CoreTeam.Roster.FirstOrDefault(s => s.Number.ToString() == action);
            if (newPlayer is null)
            {
                newPlayer = Player.Unknown(int.Parse(action));
                team.CoreTeam.AddPlayer(newPlayer);
            }
            return newPlayer;
        }

        private LineupPosition? _draggedLp;
    }
}
