using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Electrons.Net8.Controllers
{
    public class GameController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings) : ControllerBase(httpContextAccessor, env, settings)
    {

        //
        // GET: /Game/

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
        public ActionResult Live()
        {
            if (CurrentGame is null)
                return RedirectToAction("Index", "Home");
            var game = Repository.GetGameByDate(DateTime.Today);
            if (game != null)
                return View(new LiveGameModel(CurrentGame, game.GameDate, game.Location, Repository.GetSeasonHittingStats(DateTime.Today.Year, toDate: game.GameDate)));
            return View(new LiveGameModel(CurrentGame, CurrentGame.GameDate, null, null));
        }
        public ActionResult AddPitch([FromBody] PitchResult pitch)
        {
            var game = CurrentGame;
            game.AddEventToAb(Pitch.GetPitch(pitch));
            CurrentGame = game;
            return new StatusCodeResult((int)HttpStatusCode.OK);
        }
        public ActionResult UpdateGame([FromBody] string xml)
        {
            var length = CurrentContext.Request.Body.Length;
            var buffer = new byte[length];
            Request.Body.Read(buffer, 0, (int)length);
            var file = Encoding.UTF8.GetString(buffer);
            using (var sr = new StringReader(file))
            {
                var gameFile = XDocument.Load(sr);
                var game = BaseballGame.Load(gameFile);
                CurrentGame = game;
            }
            return new StatusCodeResult((int)HttpStatusCode.OK);
        }
        public BaseballGame CurrentGame
        {
            get
            {
                var game = Repository.GetGameById(GameSettings.CurrentGameId);
                return game.FullGame;
            }
            set
            {
                var dbGame = Repository.GetGameById(GameSettings.CurrentGameId);
                dbGame.SetGameFile(value.Xml);
                Repository.UpdateGame(dbGame, IsolationLevel.ReadUncommitted);
            }
        }
    }
}
