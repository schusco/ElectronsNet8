using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ScoreboardApi.Models;
using System;
using System.Collections.Generic;
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
            List<GameScore> apiData = null;
            try
            {
                apiData = await _client.GetFromJsonAsync<List<GameScore>>($"{GameSettings.BaseApiUrl}api/teams/1/Games");
                if (GameSettings.UseApiForStandings)
                    standings = await _client.GetFromJsonAsync<List<StandingsRow>>($"{GameSettings.BaseApiUrl}api/Standings/CMBA");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching standings: {ex.Message}");
                standings = new List<StandingsRow>();
            }
            return View(new MainModel(Repository, GameSettings, apiData, standings));
        }
        public ActionResult Download() => View();
        public ActionResult Electrons20() => View();
        public ActionResult Whining() => View();
        public ActionResult Login(string key)
        {
            if (key == GameSettings.AdminKey)
            {
                SetSessionObject("IsAdmin", true);
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }
    }
}
