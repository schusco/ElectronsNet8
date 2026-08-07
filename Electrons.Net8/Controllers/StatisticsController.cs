using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Electrons.Net8.Models;
using Electrons.Core.Net8.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Electrons.Net8.Controllers
{
    public class StatisticsController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env,
        IOptionsSnapshot<GameSettings> settings, ILogger<StatisticsController> logger)
        : ControllerBase(session, cache, httpContextAccessor, env, settings, logger)
    {
        [Route("statistics/{id:int?}/{type?}")]
        public async Task<IActionResult> Index(int? id, string type)
        {
            bool isPlayoffs = type is not null && (type.Equals("playoffs", StringComparison.OrdinalIgnoreCase) || type.Equals("true", StringComparison.OrdinalIgnoreCase));
            if (!id.HasValue)
                id = Repository.Seasons().First();
            var model = new StatsModel(id.Value);
            await model.Fill(Repository, isPlayoffs);
            return View(model);
        }
        [Route("statistics/{id:int}/game")]
        public ActionResult Game(int? id)
        {
            var game = Repository.GetGameById(id.Value);
            if (game is null)
                return RedirectToAction("Index", "Home");
            if (game.FullGame is null)
                return View(new GameModel(game));
            return RedirectToAction("Recap", "Game", new { id = id.Value });
        }
        [Route("statistics/{id:int}/box")]
        public ActionResult Box(int? id)
        {
            var game = Repository.GetGameById(id.Value);
            if (game is null)
                return RedirectToAction("Index", "Home");
            return View("Game", new GameModel(game));
        }
        [HttpGet, Route("statistics/records")]
        public ActionResult Records() => View("Records", new LeadersModel(GameSettings));
        public async Task<IActionResult> GetLeaders(LeadersModel model)
        {
            var hittingRecords = await Repository.GetCareerHittingStatsFromCacheAsync();
            var pitchingRecords = await Repository.GetCareerPitchingStatsFromCacheAsync();
            model.Fill(hittingRecords, pitchingRecords, GameSettings);
            return PartialView("Stats", model);
        }
        public ActionResult Export(int? id)
        {
            if (!id.HasValue && id < DateTime.Today.Year)
                return new StatusCodeResult((int)HttpStatusCode.NoContent);
            return File(ExcelGenerator.Export(id.Value, Repository), "application/download", "stats.xlsx");
        }
    }
}
