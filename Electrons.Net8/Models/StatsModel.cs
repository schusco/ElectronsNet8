using Electrons.Core.Net8;
using Electrons.Core.Net8.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Electrons.Net8.Models
{
    public class StatsModel(int year) : StatsBasedCacheModel
    {
        public IList<HittingStatsRow> HittingStats { get; set; }
        public IList<PitchingStatsRow> PitchingStats { get; set; }
        public IList<YearlySummary> HistoricalTrends { get; set; } 
        public int Year { get; set; } = year;

        internal async Task Fill(Repository repository, bool isPlayoffs)
        {
            var startYear = Year - 5;
            StatsLastUpdated = await repository.GetStatsLastUpdatedAsync();
            HittingStats = await repository.GetSeasonHittingStatsAsync(Year, isPlayoffs);
            PitchingStats = await repository.GetSeasonPitchingStatsAsync(Year, isPlayoffs);
            HistoricalTrends = await repository.GetHistoricalTrendsAsync(startYear);
            var pitchingTrends = await repository.GetPitchingTrendsAsync(startYear);
            foreach (var year in pitchingTrends)
            {
                var trend = HistoricalTrends.Single(t => t.Year == year.Year);
                if (trend != null)
                {
                    trend.TotalPitchingRuns = year.TotalRuns;
                    trend.TotalPitchingStrikeouts = year.TotalStrikeOuts;
                    trend.TotalPitchingHits = year.TotalHits;
                }
            }            
        }
    }
    
    public abstract class StatsBasedCacheModel
    {
        public DateTime StatsLastUpdated { get; set; }
    }
}