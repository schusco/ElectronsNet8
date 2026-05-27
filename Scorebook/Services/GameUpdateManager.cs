using Electrons.Core.Net8;
using ScoreboardApi.Client.Services;
using Scorebook.ViewObjects;

namespace Scorebook.Services
{
    public class GameUpdateManager
    {
        private readonly IApiService _api;
        private bool _isLoggedIn;
        private GameScoreWrapper? _selectedGame;
        public GameUpdateManager(IApiService api)
        {
            _api = api;
        }
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
        internal void SetSelectedGame(GameScoreWrapper game)
        {
            _selectedGame = game;
        }
        internal void SetNextInning()
        {
            _selectedGame?.SetNextInning();
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
    }
}
