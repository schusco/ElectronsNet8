using Electrons.Core.Net8.Infrastructure;
using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;

namespace Electrons.Net8.Controllers
{
    public class ScheduleController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings) :
        ControllerBase(session, cache, httpContextAccessor, env, settings)
    {
        [Route("schedule/{year:int?}/{month:int?}")]
        public ActionResult Index(int? month, int? year)
        {
            var actualMonth = month ?? DateTime.Today.Month;
            var actualYear = year ?? DateTime.Today.Year;
            return View(new ScheduleModel(Repository, actualMonth, actualYear));
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
