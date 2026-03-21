using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using System.Linq;

namespace Electrons.Net8.Models.Json
{
    public class GameJson
    {
        public string Opponent { get; set; }
        public string Field { get; set; }
        public string Date { get; set; }
        public string Link { get; set; }
        public string Time { get; set; }
        public int? TronScore { get; set; }
        public int? OppScore { get; set; }
        internal static GameJson Create(GameData game)
        {
            return new GameJson
            {
                Opponent = $"Electrons vs. {game.Opponent}",
                Field = game.Location.ShortFieldName,
                Date = game.GameDate.ToShortDateString(),
                Time = game.GameDate.ToShortTimeString(),
                Link = game.Location.Link,
                TronScore = game.HV == HV.H ? game.Innings.Sum(a => a.HomeRuns) : game.Innings.Sum(a => a.AwayRuns),
                OppScore = game.HV == HV.H ? game.Innings.Sum(a => a.AwayRuns) : game.Innings.Sum(a => a.HomeRuns)
            };
        }
    }
}
