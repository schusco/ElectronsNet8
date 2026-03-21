using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Electrons.Core.Net8;
using Electrons.Net8.Models;
using Electrons.Core.Net8.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Electrons.Net8.Controllers
{
    public class StatisticsController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings) : ControllerBase(httpContextAccessor, env, settings)
    {
        [Route("statistics/{id:int?}/{type?}")]
        public ActionResult Index(int? id, string type)
        {
            bool isPlayoffs;
            if (type is null)
                isPlayoffs = false;
            else
                isPlayoffs = type.Equals("playoffs", StringComparison.OrdinalIgnoreCase) || type.Equals("true", StringComparison.OrdinalIgnoreCase);
            return View(GetStatsModel(id, isPlayoffs));
        }
        public ActionResult Game(int? id)
        {
            var game = Repository.GetGameById(id.Value);
            if (game is null)
                return RedirectToAction("Index", "Home");
            if (game.FullGame is null)
                return View(new GameModel(game, CurrentContext));
            return RedirectToAction("Recap", "Game", new { id = id.Value });
        }
        public ActionResult Season(int? id, bool playoff = false) => View("Index", GetStatsModel(id, playoff));
        [HttpGet]
        public ActionResult Records() => View("Records", new LeadersModel(GameSettings));
        public ActionResult GetLeaders(LeadersModel model)
        {
            model.Fill(AllHitting, AllPitching, GameSettings);
            return PartialView("Stats", model);
        }
        public ActionResult Export(int? id)
        {
            if (!id.HasValue && id < DateTime.Today.Year)
                return new StatusCodeResult((int)HttpStatusCode.NoContent);
            return File(ExcelGenerator.Export(id.Value, Repository), "application/download", "stats.xlsx");
        }
        private StatsModel GetStatsModel(int? id, bool playoff)
        {
            if (!id.HasValue)
                id = Repository.Seasons().First();

            return new StatsModel(Repository, id.Value, playoff);
        }

        private IList<HittingStatsRow> AllHitting
        {
            get
            {
                var stats = GetSessionValue<IList<HittingStatsRow>>("allhitting");
                if (stats == null)
                {
                    stats = Repository.GetCareerHittingStats();
                    SetSessionObject("allhitting", stats);
                }
                return stats;
            }
        }
        private IList<PitchingStatsRow> AllPitching
        {
            get
            {
                var stats = GetSessionValue<IList<PitchingStatsRow>>("allpitching");
                if (stats == null)
                {
                    stats = Repository.GetCareerPitchingStats();
                    SetSessionObject("allpitching", stats);
                }
                return stats;
            }
        }
    }
}
