using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Electrons.Net8.Controllers
{
    public class RosterController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env,
        IOptionsSnapshot<GameSettings> settings, ILogger<RosterController> logger)
        : ControllerBase(session, cache, httpContextAccessor, env, settings, logger)
    {
        public ActionResult Index()
        {
            return View(new RosterModel(Repository));
        }
    }
}
