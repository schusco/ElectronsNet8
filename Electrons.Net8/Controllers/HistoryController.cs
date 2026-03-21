using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Electrons.Net8.Controllers
{
    public class HistoryController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings) : ControllerBase(httpContextAccessor, env, settings)
    {
        public ActionResult Index()
        {
            return View(new HistoryModel(Repository));
        }
    }
}
