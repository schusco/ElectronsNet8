using Electrons.Core.Net8;
using Electrons.Core.Net8.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{    
    public class LeadersModel
    {
        public LeadersModel() { }
        public LeadersModel(GameSettings settings)
        {
            HitCategories = EnumHelper.GetList<HittingCategories>().Select(s => new SelectListItem { Text = s.Key, Value = s.Value.ToString() }).ToList();
            PitchCategories = EnumHelper.GetList<PitchingCategories>().Select(s => new SelectListItem { Text = s.Key, Value = s.Value.ToString() }).ToList();
            PitchCategory = StatsCategory.Season;
            HitCategory = StatsCategory.Season;
            Thresholds = settings.ThresholdSettings;
        }
        public IList<SelectListItem> HitCategories { get; set; }
        public IList<SelectListItem> PitchCategories { get; set; }
        public int SelectedCategory { get; set; }
        public int SelectedPitchCategory { get; set; }
        public bool Hit { get; set; }
        public int Stat { get; set; }
        public string Category { get; set; }
        public StatsCategory PitchCategory { get; set; }
        public StatsCategory HitCategory { get; set; }
        public ThresholdSettings Thresholds { get; private set; }
        public string HittingThresholdString => Thresholds.HittingThresholds;
        public string PitchingThresholdString => Thresholds.PitchingThresholds;
        internal void Fill(IList<HittingStatsRow> hitting, IList<PitchingStatsRow> pitching, GameSettings settings)
        {
            int threshold;
            Thresholds = settings.ThresholdSettings;
            var statCategory = (StatsCategory)Enum.Parse(typeof(StatsCategory), Category);
            IEnumerable<LeadersRow> tempStats;
            if (Hit)
            {

                var category = (HittingCategories)Stat;
                var format = category.GetFormat<HittingStatsRow>();
                if (statCategory == StatsCategory.Career)
                {
                    threshold = Thresholds.CareerHitting;
                    tempStats = hitting.Combine().Select(s => LeadersRow.Create(s, statCategory, category, format));
                }
                else if (statCategory == StatsCategory.Season)
                {
                    threshold = Thresholds.SeasonHitting;
                    tempStats = hitting.Select(s => LeadersRow.Create(s, PitchCategory, category, format));
                }
                else
                {
                    threshold = Thresholds.PlayoffHitting;
                    tempStats = hitting.Where(w => w.Playoff).Combine().Select(s => LeadersRow.Create(s, statCategory, category, format));
                }
                tempStats = tempStats.OrderByDescending(o => o.Stat, new StatComparer());
                if (!string.IsNullOrEmpty(format))
                    tempStats = tempStats.Where(w => w.ThresholdStat > threshold);
                else
                    tempStats = tempStats.Where(w => int.Parse(w.Stat?.ToString()) > 0);
                Stats = [.. tempStats.Take(displayTotal)];
            }
            else
            {
                var category = (PitchingCategories)Stat;
                var sort = category.GetStatSort();
                var qualifier = category.HasQualifier();
                var format = category.GetFormat<PitchingStatsRow>();
                if (statCategory == StatsCategory.Career)
                {
                    threshold = Thresholds.CareerPitching;
                    var filteredStats = pitching.Combine();
                    if (qualifier)
                        filteredStats = filteredStats.Where(w => w.Innings > threshold);
                    tempStats = filteredStats.Select(s => LeadersRow.Create(s, statCategory, category, format));
                }
                else if (statCategory == StatsCategory.Season)
                {
                    threshold = Thresholds.SeasonPitching;
                    var filteredStats = pitching.Where(w => !w.Playoff);
                    if (qualifier) filteredStats = filteredStats.Where(w => w.Innings > threshold);
                    tempStats = filteredStats.Select(s => LeadersRow.Create(s, statCategory, category, format));
                }
                else
                {
                    threshold = Thresholds.PlayoffPitching;
                    var filteredStats = pitching.Where(w => w.Playoff).Combine();
                    if (qualifier) filteredStats = filteredStats.Where(w => w.Innings > threshold);
                    tempStats = filteredStats.Select(s => LeadersRow.Create(s, statCategory, category, format));
                }
                if (string.IsNullOrEmpty(format))
                    tempStats = tempStats.Where(w => int.Parse(w.Stat.ToString()) > 0);
                if (sort)
                    Stats = tempStats.OrderBy(o => o.Stat, new StatComparer()).Where(w => w.HasValue).Take(displayTotal).ToList();
                else
                    Stats = tempStats.OrderByDescending(o => o.Stat, new StatComparer()).Where(w => w.HasValue).Take(displayTotal).ToList();
            }
        }
        public IList<LeadersRow> Stats { get; set; }

        private const int displayTotal = 15;


    }
}