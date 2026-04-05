using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ScoreboardApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Electrons.Net8.Controllers
{
    public class HomeController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env,
        IOptionsSnapshot<GameSettings> settings, HttpClient client)
        : ControllerBase(session, cache, httpContextAccessor, env, settings)
    {
        private HttpClient _client = client;
        public async Task<ActionResult> Index()
        {            
            List<GameScore> apiData = await _client.GetFromJsonAsync<List<GameScore>>($"{GameSettings.BaseApiUrl}api/Games/getbydate/{DateTime.Now:yyyy-MM-dd}");
            var currentGame = apiData?.FirstOrDefault(w => w.HomeTeamId == 1 || w.AwayTeamId == 1);
            return View(new MainModel(Repository, GameSettings, WebHostEnvironment, currentGame));
        }
        public ActionResult Electrons20() => View();
        public ActionResult Whining() => View();
    }
}
