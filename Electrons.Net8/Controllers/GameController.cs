using Electrons.Core.Net8.Games;
using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ScoreboardApi.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Electrons.Net8.Controllers
{
    public class GameController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, 
        IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings, HttpClient client)
        : ControllerBase(session, cache, httpContextAccessor, env, settings)
    {
        private readonly HttpClient _client = client;
        public ActionResult Index(int? id) => RedirectToAction("Game", "Statistics", new { id });
        public ActionResult Plays(int? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index", "Home");
            var fname = Path.Combine(WebHostEnvironment.WebRootPath, $"Content/games/game{id}.sbg");
            if (System.IO.File.Exists(fname))
            {
                var game = BaseballGame.Load(fname);
                return View(new PlayByPlayModel(game, Repository, id.Value));
            }
            return RedirectToAction("Game", "Statistics", new { id });
        }
        [Route("game/{id}/Recap")]
        public ActionResult Recap(int? id)
        {
            try
            {
                if (GameSettings.WhiningToggle)
                    return RedirectToAction("Whining", "Home");
                if (!id.HasValue)
                    return RedirectToAction("Index", "Home");

                var game = Repository.GetGameById(id.Value);
                if (game is null)
                    return RedirectToAction("Index", "Home");
                if (game.GameFile != null)
                    return View("Live", new LiveGameModel(game, Repository));

                return RedirectToAction("Game", "Statistics", new { id });
            }
            catch (Exception)
            {
                return View("Error");
            }
        }
        [Route("game/{id}/Live")]
        public async Task<ActionResult> Live(int id)
        {
            var response = await _client.GetAsync($"{GameSettings.BaseApiUrl}api/Games/{id}/full");
            if (response.IsSuccessStatusCode)
            {
                var game = await response.Content.ReadAsAsync<GameScore>();
                return View(new LiveGameModel(game));
            }
            return View("Error");
        }
    }
}
