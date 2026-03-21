using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using Electrons.Core.Net8.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class StatsModel
    {
        public StatsModel(Repository repo, int year, bool playoffs)
        {
            Year = year;
            HittingStats = [.. repo.GetSeasonHittingStats(year, playoffs)];
            HittingStats.Cast<Electrons.Core.Net8.Games.IHasPlayer>().SetDuplicatePlayers();
            PitchingStats = [.. repo.GetSeasonPitchingStats(year, playoffs)];
        }
        public IList<HittingStatsRow> HittingStats { get; set; }
        public IList<PitchingStatsRow> PitchingStats { get; set; }
        public int Year { get; set; }
    }
}