using ScoreboardApi.Client.Services;
using ScoreboardApi.Models;
using Scorebook.ViewObjects;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;


namespace Scorebook.Services
{
    public class ApiService : ServiceBase
    {
        internal static readonly IDictionary<string, List<CmbaPlayer>> ApiRosters = new Dictionary<string, List<CmbaPlayer>>();
        public static ObservableCollection<Team> ApiTeams { get; set; } = [];
        public ApiService(IHttpClientFactory factory, IApiService apiService) : base(factory) { }
        public async Task<List<GameScore>> GetSchedule(int teamId)
        {
            DateTime nextSync = Preferences.Default.Get($"ScheduleNextSync_{teamId}", DateTime.MinValue);
            var games = new List<GameScore>();
            if (DateTime.Now < nextSync)
                games = LoadFromLocalDisk<List<GameScore>>($"schedule_cache_{teamId}.json");

            if (games is null || games.Count == 0)
            {
                var response = await LoadFromApi<List<GameScore>>($"/api/teams/{teamId}/games");
                games = response.Success ? response.Data : new List<GameScore>();
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

            if (teams is null || teams.Count == 0)
            {
                var response = await LoadFromApi<List<Team>>($"/api/teams");
                teams = response.Success ? response.Data : new List<Team>();
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
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await LoadFromApi<List<CmbaPlayer>>($"/api/teams/{teamId}/roster", cts.Token);
                roster = response.Success ? response.Data : new List<CmbaPlayer>();
                if (roster.Any())
                {
                    SaveToLocalDisk(roster, $"roster_cache_{teamId}.json");
                    Preferences.Default.Set($"RosterNextSync_{teamId}", DateTime.Now.AddDays(7));
                }
            }
            return roster;
        }
        public async Task SendGameUpdate(object sender, GameScoreEventArgs e)
        {
            var client = GetAuthClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                var response = await client.PatchAsJsonAsync($"api/games/{e.Game.GameId}", e.Game);                
            }
            catch (OperationCanceledException)
            {
                // Handle timeout or cancellation gracefully            
            }
        }
        internal async Task<List<CmbaPlayer>> GetRosterFromApi(Team team, bool forceRefresh = false)
        {
            if (ApiRosters.TryGetValue(team.Name, out var hroster) && !forceRefresh)
                return hroster;
            var roster = await GetRoster(team.Id, forceRefresh);
            ApiRosters[team.Name] = roster;
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
        private async Task<ApiResponse<T>> LoadFromApi<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetPublicClient();

                var response = await client.GetAsync(endpoint, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                    return new ApiResponse<T> { Success = true, Data = data };
                }
                return new ApiResponse<T> { Success = false, Message = "Request failed" };

            }
            catch (OperationCanceledException)
            {
                // Handle timeout or cancellation gracefully
                return new ApiResponse<T> { Success = false, Message = "Request timed out" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Message = ex.Message };
            }
        }
        public static void LoadRosters()
        {
            foreach (var team in ApiTeams)
            {
                var roster = LoadCachedRoster(team.Id);
                if (roster != null && roster.Any())
                    ApiRosters.Add(team.Name, roster);
            }
        }
        private static void SaveToLocalDisk<T>(List<T> data, string path)
        {
            string localPath = Path.Combine(FileSystem.Current.AppDataDirectory, path);
            if (data.Count > 0)
            {
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(localPath, json);
            }
        }
        internal static List<CmbaPlayer> LoadCachedRoster(int teamId)
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
        internal async Task<GameScore?> GetGame(int id)
        {
            var response = await LoadFromApi<GameScore>($"api/games/{id}");
            if (response.Success)
                return response.Data;
            return null;
        }
    }
    internal class ApiResponse<T>
    {
        public T Data { get; set; }
        public string? Message { get; set; }
        public bool Success { get; set; }
    }

}
