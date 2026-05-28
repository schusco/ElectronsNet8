using Electrons.Core.Net8.Games;
using ScoreboardApi.Client.Services;
using ScoreboardApi.Models;
using Scorebook.ViewObjects;

namespace Scorebook.Services
{
    public class GameUpdateManager(IApiService api)
    {
        private readonly IApiService _api = api;
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
            var response = await _api.Login(user, pass);
            _isLoggedIn = response.Success;
        }
        internal GameScoreWrapper? SelectedGame => _selectedGame;
        internal void SetSelectedGame(GameScoreWrapper game)
        {
            _selectedGame = game;
        }
        internal void SetNextInning(AtBat currentAb)
        {
            _selectedGame?.SetNextInning(currentAb);
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

        internal void UpdateAb(Atbat ab)
        {
            _selectedGame?.UpdateAb(ab);
        }
        internal void UpdateAb(AtBat ab)
        {
            _selectedGame?.UpdateAb(ab);
        }

        internal void UpdateInning(Electrons.Core.Net8.Games.Inning currentInning)
        {
            _selectedGame?.UpdateInning(currentInning.Runs, currentInning.Hits, currentInning.Errors);
        }
    }
}
