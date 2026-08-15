using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace Electrons.Net8.Controllers
{
    public class ProfileController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env,
        IOptionsSnapshot<GameSettings> settings, ILogger<ProfileController> logger)
        : ControllerBase(session, cache, httpContextAccessor, env, settings, logger)
    {
        public ActionResult Index(int? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index");
            var profile = Repository.GetPlayer(id.Value);
            if (profile is null || profile.Nickname == "XX")
                return RedirectToAction("Index", "Roster");
            var model = new ProfileModel(profile, Repository.GetCareerHittingStats(id.Value), Repository.GetCareerPitchingStats(id.Value));
            model.ImageFile = System.IO.File.Exists(Path.Combine(WebHostEnvironment.WebRootPath, model.ImageFile)) ? model.ImageFile : "Content/images/players/NotAvailable.png";
            return View(model);
        }
    }
}
