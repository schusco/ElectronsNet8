using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Electrons.Net8.Controllers
{
    public class HistoryController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings)
        : ControllerBase(session, cache, httpContextAccessor, env, settings)
    {
        public async Task<IActionResult> Index()
        {
            var lastUpdate = await Repository.GetStatsLastUpdatedAsync();
            var model = new HistoryModel(Repository)
            {
                StatsLastUpdated = lastUpdate
            };
            return View(model);
        }
    }
}
