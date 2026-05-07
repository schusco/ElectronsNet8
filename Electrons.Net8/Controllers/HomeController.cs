using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ScoreboardApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Electrons.Net8.Controllers
{
    public class HomeController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor context, IWebHostEnvironment env,
        IOptionsSnapshot<GameSettings> settings, HttpClient client) : ControllerBase(session, cache, context, env, settings)
    {
        private readonly HttpClient _client = client;
        public async Task<ActionResult> Index()
        {
            List<StandingsRow> standings = null;
            List<GameScore> apiData = await _client.GetFromJsonAsync<List<GameScore>>($"{GameSettings.BaseApiUrl}api/Games/getbydate/{DateTime.Now:yyyy-MM-dd}");
            try
            {
                if (GameSettings.UseApiForStandings)
                    standings = await _client.GetFromJsonAsync<List<StandingsRow>>($"{GameSettings.BaseApiUrl}api/Standings/CMBA");
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
                Console.WriteLine($"Error fetching standings: {ex.Message}");
                // Optionally, you can set standings to an empty list or null to handle it gracefully in the view
                standings = new List<StandingsRow>();
            }
            var currentGame = apiData?.FirstOrDefault(w => w.HomeTeamId == 1 || w.AwayTeamId == 1);
            return View(new MainModel(Repository, GameSettings, WebHostEnvironment, currentGame, standings));
        }
        public ActionResult Download() => View();
        public ActionResult Electrons20() => View();
        public ActionResult Whining() => View();
    }
}
