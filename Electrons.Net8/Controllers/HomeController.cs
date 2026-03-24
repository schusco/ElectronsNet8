using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.IO;

namespace Electrons.Net8.Controllers
{
    public class HomeController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings)
        : ControllerBase(session,cache, httpContextAccessor, env, settings)
    {
        public ActionResult Index() => View(new MainModel(Repository, GameSettings, WebHostEnvironment));
        public ActionResult Electrons20() => View();
        public ActionResult Whining() => View();
    }
}
