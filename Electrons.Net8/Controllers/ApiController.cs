using Electrons.Net8.Models.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Electrons.Net8.Controllers
{
    public class ApiController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings) : ControllerBase(httpContextAccessor, env, settings)
    {
        public ActionResult GetUpcomingGames()
        {
            var games = Repository.GetGamesByYear(DateTime.Now.Year);
            return Json(games.Select(GameJson.Create));
        }
    }
}
