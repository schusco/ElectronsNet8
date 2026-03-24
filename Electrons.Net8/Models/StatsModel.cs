using Electrons.Core.Net8;
using Electrons.Core.Net8.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Electrons.Net8.Models
{
    public class StatsModel(int year) : StatsBasedCacheModel
    {
        public IList<HittingStatsRow> HittingStats { get; set; }
        public IList<PitchingStatsRow> PitchingStats { get; set; }

        public int Year { get; set; } = year;

        internal async Task Fill(Repository repository, bool isPlayoffs)
        {
            StatsLastUpdated = await repository.GetStatsLastUpdatedAsync();
            HittingStats = await repository.GetSeasonHittingStatsAsync(Year, isPlayoffs);
            PitchingStats = await repository.GetSeasonPitchingStatsAsync(Year, isPlayoffs);
        }
    }
    public abstract class StatsBasedCacheModel
    {
        public DateTime StatsLastUpdated { get; set; }
    }
}