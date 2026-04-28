using Microsoft.Extensions.Configuration;
using ScoreboardApi.Models;
using System.Net.Http.Json;


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
            using (var client = new HttpClient() { BaseAddress = _apiBaseUrl })
            {
                var games = await client.GetFromJsonAsync<List<GameScore>>($"/api/teams/{teamId}/games");
                return games;
            }
        }
        public async Task<List<Team>> GetTeams()
        {
            using (var client = new HttpClient() { BaseAddress = _apiBaseUrl })
            {
                var teams = await client.GetFromJsonAsync<List<Team>>($"/api/teams");
                return teams;
            }
        }
        public async Task<List<CmbaPlayer>> GetRoster(int teamId)
        {
            using (var client = new HttpClient() { BaseAddress = _apiBaseUrl })
            {
                var roster = await client.GetFromJsonAsync<List<CmbaPlayer>>($"/api/teams/{teamId}/roster");
                return roster;
            }
        }
    }
}
