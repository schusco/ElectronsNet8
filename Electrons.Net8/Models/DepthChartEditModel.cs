using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Electrons.Net8.Models
{
    public class DepthChartEditModel
    {
        public DepthChartEditModel() { }
        public DepthChartEditModel(IList<PlayerProfile> players, IDictionary<DcPosition, List<DepthChart>> depthChart)
        {
            Players = [.. players.Select(s => new SelectListItem { Text = s.FullName, Value = s.Id.ToString() })];
            Position = depthChart.Keys.FirstOrDefault();
            DepthChart = depthChart[Position];
            PositionString = Position.GetDescription();
        }        
        public string Add { get; set; }
        public IEnumerable<SelectListItem> Players { get; private set; }
        public DcPosition Position { get; set; }
        public string PositionString { get; set; }
        public List<DepthChart> DepthChart { get; set; }
    }
}
