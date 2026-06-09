using Electrons.Core.Net8;
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
using System.Linq;
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
        [Route("game/{id}/Box")]
        public ActionResult Box(int? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index", "Home");
            var game = Repository.GetGameById(id.Value);
            if (game is null)
                return RedirectToAction("Index", "Home");
            return RedirectToAction("Box", "Statistics", new { id = id.Value });
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
        public async Task<ActionResult> GetPlayByPlayPartial(int gameId)
        {
            var data = await GetCachedGameData(gameId);
            return PartialView("PlayByPlay", data.PlayByPlay);
        }
        public async Task<ActionResult> GetScoringPlaysPartial(int gameId)
        {
            var data = await GetCachedGameData(gameId);
            return PartialView("ScoringPlay", data.ScoringPlays);
        }
        public async Task<ActionResult> GetBoxScorePartial(int gameId, bool home)
        {
            var data = await GetCachedGameData(gameId);
            ViewData.Add("home", home);
            ViewData.Add("logo", home ? data.HomeTeamName.GetLogo() : data.AwayTeamName.GetLogo());
            ViewData.Add("team", home ? data.HomeTeamName : data.AwayTeamName);
            return PartialView("BoxScore", home ? data.HomeBoxScore : data.AwayBoxScore);
        }
        private async Task<GameInningUpdateDto> GetCachedGameData(int gameId)
        {
            string cacheKey = $"game_history_{gameId}";

            // Try to get the data from server memory. If it's missing, execute the factory block:
            return await Cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                // Set a strict expiration window so it never stays stale
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);
                var response = await _client.GetAsync($"{GameSettings.BaseApiUrl}api/Games/{gameId}/full");
                if (response.IsSuccessStatusCode)
                {
                    var game = await response.Content.ReadAsAsync<GameScore>();

                    // Fetch the heavy data from the database ONCE
                    return new GameInningUpdateDto
                    {
                        GameId = gameId,
                        HomeTeamName = game.HomeTeam?.Name ?? "Home Team",
                        AwayTeamName = game.AwayTeam?.Name ?? "Away Team",
                        PlayByPlay = InningModel.CreateInnings(game.Innings.ToList(), game.HomeTeam.Name.GetLogo(), game.AwayTeam.Name.GetLogo()),
                        HomeBoxScore = HomeBoxScore.Create(game),
                        AwayBoxScore = AwayBoxScore.Create(game),
                        ScoringPlays = ScoringPlayModel.CreateScoringPlays(game.Innings.ToList(), game.HomeTeam.Name.GetLogo(), game.AwayTeam.Name.GetLogo())
                    };
                }
                throw new Exception($"Failed to fetch game data for game ID {gameId}");
            });
        }
    }
}
