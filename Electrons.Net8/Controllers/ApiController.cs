using Electrons.Net8.Models.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Linq;


namespace Electrons.Net8.Controllers
{
    public class ApiController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings)
        : ControllerBase(session, cache, httpContextAccessor, env, settings)
    {
        public ActionResult GetUpcomingGames()
        {
            var games = Repository.GetGamesByYear(DateTime.Now.Year);
            return Json(games.Select(GameJson.Create));
        }
        [AllowAnonymous, HttpPost("admin/clear-cache")]
        public IActionResult ClearRecordsCache([FromHeader(Name = "X-Api-Key")] string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey != GameSettings.ApiKey)
                return Unauthorized();
            Repository.ResetRecordsCache();
            return Ok(new { success = true, message = "Cache cleared by remote update process" });
        }
    }
}
