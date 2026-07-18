using Electrons.Core.Net8.Infrastructure;
using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScoreboardApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Electrons.Net8.Controllers
{
    public class ScheduleController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env,
        IOptionsSnapshot<GameSettings> settings, ILogger<ScheduleController> logger, HttpClient client) :
        ControllerBase(session, cache, httpContextAccessor, env, settings, logger)
    {
        private readonly HttpClient _client = client;
        [Route("schedule/{year:int?}/{month:int?}")]
        public async Task<ActionResult> Index(int? month, int? year)
        {
            List<GameScore> apidata = new List<GameScore>();
            var actualMonth = month ?? DateTime.Today.Month;
            var actualYear = year ?? DateTime.Today.Year;
            if (actualYear == DateTime.Now.Year)
            {
                if (Cache.TryGetValue($"ApiData", out apidata));
                    apidata = apidata?.Where(w => w.GameDate.Month == actualMonth).ToList();
                if (apidata.Count == 0)
                    apidata = await _client.GetFromJsonAsync<List<GameScore>>($"{GameSettings.BaseApiUrl}api/teams/1/Games/{actualMonth}");
            }
            return View(new ScheduleModel(Repository, actualMonth, actualYear, apidata));
        }
        public ActionResult Download()
        {
            var schedule = PdfGenerator.Schedule(Repository, DateTime.Now.Year);
            return File(schedule, "application/download", "ElectronSchedule.pdf");
        }

        public ActionResult Google()
        {
            return View("Schedulex");
        }

        public ActionResult Group()
        {
            return View();
        }
    }
}
