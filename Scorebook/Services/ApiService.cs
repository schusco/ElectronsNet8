using Microsoft.Extensions.Logging;
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
        private readonly IApiService _apiService;
        private readonly ILogger<ApiService> _logger;
        internal static readonly IDictionary<string, List<CmbaPlayer>> ApiRosters = new Dictionary<string, List<CmbaPlayer>>();
        public static ObservableCollection<Team> ApiTeams { get; set; } = [];
        public ApiService(IHttpClientFactory factory, IApiService apiService, ILogger<ApiService> logger) : base(factory)
        {
            _apiService = apiService;
            _logger = logger;
        }
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
            return teams.Where(w => w.Current).ToList();
        }
        public async Task<List<CmbaPlayer>> GetRoster(int teamId, bool forceRefresh)
        {
            DateTime nextSync = Preferences.Default.Get($"RosterNextSync_{teamId}", DateTime.MinValue);
            var roster = new List<CmbaPlayer>();
            if (DateTime.Now < nextSync && !forceRefresh)
                roster = LoadFromLocalDisk<List<CmbaPlayer>>($"roster_cache_{teamId}.json");
            else
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
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
        internal async Task<GameScore?> GetFullGame(int gameId)
        {
            var response = await LoadFromApi<GameScore>($"/api/games/{gameId}/full");
            var fg = response.Success ? response.Data : null;
            return fg;
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
        internal async Task<Inning?> SendInningUpdate(Inning inning)
        {
            var url = $"api/innings/{inning.Id}";
            if (inning.Id == 0)
                url = $"api/games/{inning.GameId}/innings";
            return await SendUpdate(url, inning, inning.Id == 0);
        }
        private async Task<T?> SendUpdate<T>(string url, T? data, bool post) where T : class
        {
            HttpResponseMessage response;
            var client = GetAuthClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var json = JsonSerializer.Serialize(data);
            try
            {
                // 1. Execute the correct HTTP method and capture the response
                if (post)
                    response = await client.PostAsJsonAsync(url, data, cts.Token);
                else
                    response = await client.PutAsJsonAsync(url, data, cts.Token);

                // 2. Unify the response handling for BOTH POST and PUT
                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                        return data;
                    var respObj = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cts.Token);
                    return respObj;
                }
                else
                    _logger.LogWarning("Request failed. Method: {Method}, StatusCode: {StatusCode}, Payload: {json}",
                        post ? "POST" : "PUT", response.StatusCode, json);

            }
            catch (OperationCanceledException)
            {
                // If cts.Token canceled it, it's a timeout
                _logger.LogError("The network request to {Url} timed out after 15 seconds.", url);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected network/deserialization crashes
                _logger.LogError(ex, "An unexpected error occurred during SendUpdate for {Url}", url);
            }
            return null;
        }
        internal async Task<Atbat?> SendAbUpdate(Atbat ab)
        {
            var url = $"api/atbats/{ab.Id}";
            if (ab.Id == 0)
                url = $"api/innings/{ab.InningId}/ab";
            return await SendUpdate(url, ab, ab.Id == 0);
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
