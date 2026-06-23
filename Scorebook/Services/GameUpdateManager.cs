using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using ScoreboardApi.Client.Services;
using ScoreboardApi.Models;
using Scorebook.ViewObjects;
using System.Threading.Tasks;
using ApiAb = ScoreboardApi.Models.Atbat;
using ApiInning = ScoreboardApi.Models.Inning;

namespace Scorebook.Services
{
    public class GameUpdateManager(IApiService api, ApiService apiService)
    {
        private readonly IApiService _api = api;
        private readonly ApiService _apiService = apiService;
        private ScorebookViewModel _vm;
        private bool _isLoggedIn;
        private GameScoreWrapper? _selectedGame;

        public bool IsLoggedIn => _isLoggedIn;
        public async Task StartAsync()
        {
            var user = await SecureStorage.Default.GetAsync("service_user");
            var pass = await SecureStorage.Default.GetAsync("service_pwd");

            if (user is null || pass is null)
            {
                user = Constants.UserId;
                pass = Constants.Pwd;
                await SecureStorage.Default.SetAsync("service_user", user);
                await SecureStorage.Default.SetAsync("service_pwd", pass);
            }
            try
            {
                var response = await _api.Login(user, pass);
                _isLoggedIn = response.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
                _isLoggedIn = false;
            }
        }
        internal GameScoreWrapper? SelectedGame => _selectedGame;
        internal void SetSelectedGame(GameScoreWrapper game, ScorebookViewModel vm)
        {
            _selectedGame = game;
            _vm = vm;
            _halfInning = HalfInning.Top;
            _inningNumber = 0;
            _currentInning = null;
            _currentAtbat = null;
        }
        internal async Task SetNextInning()
        {
            if (_selectedGame is null)
                return;
            if (_inningNumber == 0)
            {
                _halfInning = HalfInning.Top;
                _inningNumber = 1;
            }
            else if (_halfInning == HalfInning.Top)
                _halfInning = HalfInning.Bottom;
            else
            {
                _halfInning = HalfInning.Top;
                _inningNumber++;
            }
            _selectedGame?.SetInningStatus($"{_halfInning} of {_inningNumber}");
            _selectedGame?.OnGameScoreUpdated();
            _currentInning = new ApiInning
            {
                GameId = _selectedGame.GameId,
                Errors = 0,
                Hits = 0,
                Number = _inningNumber,
                Runs = 0,
                IsTopHalf = _halfInning == HalfInning.Top
            };
            var result = await _apiService.SendInningUpdate(_currentInning);
            if (result != null)
            {
                _currentInning = result;
                if (_currentInning != null)
                    await UpdateAb(ConvertAb(_vm.Game.CurrentAb));
            }
        }
        internal void SetStartDateTime(DateTime? startTime)
        {
            _selectedGame?.SetStartDateTime(startTime);
        }
        internal void UpdateScore(int homeScore, int awayScore)
        {
            _selectedGame?.SetScore(homeScore, awayScore);
        }
        internal void SetGameEnded(DateTime? endTime)
        {
            _selectedGame?.SetGameEnded(endTime);
        }
        internal async Task UpdateAb(ApiAb ab)
        {
            _currentAtbat = ab;
            await OnAtBatUpdated();
        }
        internal async Task UpdateAb(AtBat? ab)
        {
            if (_currentInning != null)
                await UpdateAb(ConvertAb(ab));
        }
        internal async Task UpdateInning(Electrons.Core.Net8.Games.Inning currentInning)
        {
            await UpdateInning(currentInning.Runs, currentInning.Hits, currentInning.Errors);
        }
        private async Task OnAtBatUpdated()
        {
            if (_currentAtbat is null) return;
            if (_currentInning is null) return;
            if (_currentInning.Id > 0)
            {
                _currentAtbat.InningId = _currentInning.Id;
                var result = await _apiService.SendAbUpdate(_currentAtbat);
                if (result != null)
                {
                    _currentAtbat = result;
                    CheckPlayers(_currentAtbat);
                }
            }
        }
        private void CheckPlayers(ApiAb ab)
        {
            if (ab.BatterId > 0 && ab.Batter != null)
            {
                var roster = _selectedGame.HomeTeamId == ab.Batter.TeamId ? ApiService.ApiRosters[_selectedGame.HomeTeam.Name]
                    : ApiService.ApiRosters[_selectedGame.AwayTeam.Name];
                if (!roster.Any(a => a.Id == ab.BatterId))
                    roster.Add(ab.Batter);
            }
            if (ab.PitcherId > 0 && ab.Pitcher != null)
            {
                var roster = _selectedGame.HomeTeamId == ab.Pitcher?.TeamId ? ApiService.ApiRosters[_selectedGame.HomeTeam.Name]
                    : ApiService.ApiRosters[_selectedGame.AwayTeam.Name];
                if (!roster.Any(a => a.Id == ab.PitcherId))
                    roster.Add(ab.Pitcher);
            }
        }
        private async Task UpdateInning(int runs, int hits, int errors)
        {
            if (_currentInning == null)
                return;
            if (_currentInning.Runs == runs && _currentInning.Hits == hits && _currentInning.Errors == errors && _currentInning.Id != 0)
                return;
            _currentInning.Runs = runs;
            _currentInning.Hits = hits;
            _currentInning.Errors = errors;
            var result = await _apiService.SendInningUpdate(_currentInning);
            if (result != null)
                _currentInning.Id = result.Id;
        }
        private ApiAb? ConvertAb(AtBat? currentAb)
        {
            if (_currentInning is null)
                return null;
            var hittingTeam = _currentInning.IsTopHalf ? _selectedGame?.AwayTeam : _selectedGame?.HomeTeam;
            var pitchingTeam = _currentInning.IsTopHalf ? _selectedGame?.HomeTeam : _selectedGame?.AwayTeam;
            var batter = ApiService.ApiRosters[hittingTeam.Name].FirstOrDefault(p => p.Number == currentAb?.Batter.Number);
            var pitcher = ApiService.ApiRosters[pitchingTeam.Name].FirstOrDefault(p => p.Number == currentAb?.Pitcher.Number);

            var batterId = batter?.Id ?? 0;
            var pitcherId = pitcher?.Id ?? 0;
            var ab = new ApiAb
            {
                Id = _currentAtbat?.Sequence != currentAb?.Sequence ? 0 : _currentAtbat.Id,
                Sequence = currentAb.Sequence,
                BatterId = batterId,
                PitcherId = pitcherId,
                Batter = batterId > 0 ? null : new CmbaPlayer
                {
                    FirstName = currentAb.Batter.FirstName,
                    LastName = currentAb.Batter.LastName,
                    Number = currentAb.Batter.Number,
                    TeamId = hittingTeam.Id
                },
                Pitcher = pitcherId > 0 ? null : new CmbaPlayer
                {
                    FirstName = currentAb.Pitcher.FirstName,
                    LastName = currentAb.Pitcher.LastName,
                    Number = currentAb.Pitcher.Number,
                    TeamId = pitchingTeam.Id
                },
                Result = currentAb.ToString(),
                Balls = currentAb.Balls > 3 ? 3 : currentAb.Balls,
                Strikes = currentAb.Strikes > 2 ? 2 : currentAb.Strikes,
                Outs = _vm.Game.CurrentInning.Outs,
                Scoring = currentAb.Runs,
                OnBase = (int)_vm.Game.CurrentInning.CurrentRunners.Runners
            };
            return ab;
        }
        internal async Task Refresh()
        {
            var fg = await _apiService.GetFullGame(_selectedGame.GameId);
            _currentInning = fg.Innings.LastOrDefault();
            _currentAtbat = _currentInning?.Atbats.LastOrDefault();
            _inningNumber = _currentInning.Number;
            _halfInning = _currentInning.IsTopHalf ? HalfInning.Top : HalfInning.Bottom;
        }

        private HalfInning _halfInning;
        private int _inningNumber;
        private ApiInning? _currentInning;
        private ApiAb? _currentAtbat;
    }
}
