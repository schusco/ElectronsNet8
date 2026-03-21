using System;

namespace Electrons.Core.Net8
{
    public class LeadersRow
    {
        private LeadersRow()
        {

        }
        [TableColumn(Optional = true)]
        public int? Season { get; set; }
        [TableColumn]
        public string Player => $"{LastName}, {FirstName.Substring(0, 1)}";
        [TableColumn(HeaderProperty = "Header")]
        public object Stat
        {
            get
            {
                if (_stat is decimal && !string.IsNullOrEmpty(Format))
                    return ((decimal)_stat).ToString(Format);
                return _stat;
            }
            set => _stat = value;
        }
        public decimal ThresholdStat { get; set; }
        public string FirstName { get; internal set; }
        public string LastName { get; internal set; }
        public string Header { get; internal set; }
        public string Format { get; set; }
        public override string ToString() => $"{Player},({Season}), {Stat}";
        public static LeadersRow Create(HittingStatsRow arg, StatsCategory cat, HittingCategories stat, string format)
        {
            return new LeadersRow
            {
                Season = cat == StatsCategory.Season ? arg.Year : (int?)null,
                FirstName = arg.FirstName,
                LastName = arg.LastName,
                Header = stat.GetPropertyDisplayName(),
                Stat = arg.GetPropertyValue(stat),
                ThresholdStat = arg.AtBats,
                Format = format
            };
        }
        public static LeadersRow Create(PitchingStatsRow arg, StatsCategory cat, PitchingCategories stat, string format)
        {
            return new LeadersRow
            {
                Season = cat == StatsCategory.Season ? arg.Year : (int?)null,
                FirstName = arg.FirstName,
                LastName = arg.LastName,
                Header = stat.GetPropertyDisplayName(),
                Stat = arg.GetPropertyValue(stat),
                ThresholdStat = arg.Innings,
                Format = format
            };
        }
        public bool HasValue => !(_stat is null) && Convert.ToDecimal(_stat) > 0;

        private object _stat;
    }
}