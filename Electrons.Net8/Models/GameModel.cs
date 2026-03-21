using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class GameModel
    {
        public GameModel(GameData game, HttpContext context)
        {
            GameId = game.GameId;
            DateAndLocation = $"{game.GameDate.ToLongDateString()}  {game.GameDate.ToShortTimeString()} @ {game.Location}";
            AwayLine = game.GetLineScore(HV.V);
            HomeLine = game.GetLineScore(HV.H);
            var request= context.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            var elecUrl = $"{baseUrl}/Content/Images/logos/nextOuting_electrons.png";
            var oppUrl = $"{baseUrl}/Content/images/logos/nextOuting_{game.Opponent.Replace(" ", "").ToLower()}.png";
            if (AwayLine.Team == "Electrons")
            {
                AwayLogo = elecUrl;
                HomeLogo = oppUrl;
            }
            else
            {
                HomeLogo = elecUrl;
                AwayLogo = oppUrl;
            }
            PitchingStats = game.PitchingStats.Select(s => new PitchingStatsRow(s, true, true, false)).ToList();
            foreach (var item in PitchingStats)
                item.Games = null;
            HittingStats = game.HittingStats.Where(w => w.Profile.Nickname != "XX").Select(s => new HittingStatsRow(s, true, true)).ToList();
            foreach (var item in HittingStats)
                item.Games = null;
            Notes = game.Notes;
            HasPlayByPlay = game.FullGame != null;
        }
        public int GameId { get; set; }
        public string DateAndLocation { get; set; }
        public string AwayLogo { get; set; }
        public string HomeLogo { get; set; }
        public int Innings => HomeLine.Innings.Count();
        public LineScoreModel HomeLine { get; set; }
        public LineScoreModel AwayLine { get; set; }
        public IList<PitchingStatsRow> PitchingStats { get; set; }
        public IList<HittingStatsRow> HittingStats { get; set; }
        public string Notes { get; set; }
        public bool HasPlayByPlay { get; set; }
    }
}