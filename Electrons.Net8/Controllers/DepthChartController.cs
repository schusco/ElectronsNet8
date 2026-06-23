using Electrons.Core.Net8;
using Electrons.Core.Net8.Infrastructure.Dto;
using Electrons.Net8.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Electrons.Net8.Controllers
{
    public class DepthChartController(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env,
        IOptionsSnapshot<GameSettings> settings, ILogger<DepthChartController> logger)
        : ControllerBase(session, cache, httpContextAccessor, env, settings, logger)
    {
        public ActionResult Index() => View(new DepthChartModel(Repository, IsAdmin));
        [HttpGet, Route("DepthChart/Edit/{pos:int}")]
        public ActionResult Edit(int pos)
        {
            if (!IsAdmin)
                return RedirectToAction("Index");
            if (!System.Enum.IsDefined(typeof(DcPosition), pos))
                return BadRequest("Invalid position");
            var depthChart = Repository.GetDepthChart(pos);
            if (depthChart == null)
                return NotFound("Depth chart not found");
            return View(new DepthChartEditModel(Repository.GetCurrentPlayers(), depthChart));
        }
        [HttpPost, Route("DepthChart/Update/{pos:int}")]
        public ActionResult UpdateDepthChart(int pos, [FromForm] DepthChartEditModel dcs)
        {
            if (!System.Enum.IsDefined(typeof(DcPosition), pos))
                return BadRequest("Invalid position");
            if (!string.IsNullOrEmpty(dcs.Add))
            {
                Repository.CreateDepthChart(new DepthChartDto((DcPosition)pos, 0, dcs.DepthChart.Count + 1));
                return RedirectToAction("Edit", new { pos });
            }
            else
            {
                var success = Repository.UpdateDepthChart([.. dcs.DepthChart.Select(s => new DepthChartDto((DcPosition)pos, s.PlayerId, s.Rank))]);
                if (!success)
                    return BadRequest("Failed to update depth chart");
                return RedirectToAction("Index");
            }
        }
        public ActionResult DeleteDepthChart(int id)
        {
            var success = Repository.DeleteDepthChart(id);
            if (!success)
                return BadRequest("Failed to delete depth chart");
            return RedirectToAction("Index");
        }
    }
}
