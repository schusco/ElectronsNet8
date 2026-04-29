using Microsoft.Extensions.Configuration;
using ScoreboardApi.Models;
using System.Net.Http.Json;
using System.Text.Json;


namespace Scorebook.Services
{
    public class ApiService
    {
        private readonly IConfiguration _config;
        private readonly Uri _apiBaseUrl;
        public ApiService(IConfiguration config)
        {
            _config = config;
            _apiBaseUrl = new Uri(_config.GetValue<string>("ApiBaseUrl"));
        }
        public async Task<List<GameScore>> GetSchedule(int teamId)
        {
            DateTime nextSync = Preferences.Default.Get($"ScheduleNextSync_{teamId}", DateTime.MinValue);
            var games = new List<GameScore>();
            if (DateTime.Now < nextSync)
                games = LoadFromLocalDisk<List<GameScore>>($"schedule_cache_{teamId}.json");
            else
            {
                games = await LoadFromApi<List<GameScore>>($"/api/teams/{teamId}/games");
                SaveToLocalDisk(games, $"schedule_cache_{teamId}.json");
                Preferences.Default.Set($"ScheduleNextSync_{teamId}", DateTime.Now.AddDays(1));
            }
            return games;
        }
        public async Task<List<Team>> GetTeams()
        {
            DateTime nextSync = Preferences.Default.Get("TeamsNextSync", DateTime.MinValue);
            var teams = new List<Team>();
            if (DateTime.Now < nextSync)
                teams = LoadFromLocalDisk<List<Team>>("teams_cache.json");
            else
            {
                teams = await LoadFromApi<List<Team>>($"/api/teams");
                SaveToLocalDisk(teams, "teams_cache.json");
                Preferences.Default.Set("TeamsNextSync", DateTime.Now.AddYears(1));
            }
            return teams;
        }
        public async Task<List<CmbaPlayer>> GetRoster(int teamId, bool forceRefresh)
        {
            DateTime nextSync = Preferences.Default.Get($"RosterNextSync_{teamId}", DateTime.MinValue);
            var roster = new List<CmbaPlayer>();
            if (DateTime.Now < nextSync && !forceRefresh)
                roster = LoadFromLocalDisk<List<CmbaPlayer>>($"roster_cache_{teamId}.json");
            else
            {
                roster = await LoadFromApi<List<CmbaPlayer>>($"/api/teams/{teamId}/roster");
                if (roster.Any())
                {
                    SaveToLocalDisk(roster, $"roster_cache_{teamId}.json");
                    Preferences.Default.Set($"RosterNextSync_{teamId}", DateTime.Now.AddDays(7));
                }
            }
            return roster;
        }
        private static T LoadFromLocalDisk<T>(string path)
        {
            var localPath = Path.Combine(FileSystem.Current.AppDataDirectory, path);
            if (!File.Exists(localPath))
                return default;
            var json = File.ReadAllText(localPath);
            return JsonSerializer.Deserialize<T>(json);
        }
        private async Task<T> LoadFromApi<T>(string endpoint)
        {
            using (var client = new HttpClient() { BaseAddress = _apiBaseUrl })
            {
                return await client.GetFromJsonAsync<T>(endpoint);
            }
        }
        private void SaveToLocalDisk<T>(List<T> teams, string path)
        {
            string localPath = Path.Combine(FileSystem.Current.AppDataDirectory, path);
            var json = JsonSerializer.Serialize(teams);
            File.WriteAllText(localPath, json);
        }

        internal List<CmbaPlayer> LoadCachedRoster(int teamId)
        {
            try
            {
                var roster = LoadFromLocalDisk<List<CmbaPlayer>>($"roster_cache_{teamId}.json");
                return roster;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
