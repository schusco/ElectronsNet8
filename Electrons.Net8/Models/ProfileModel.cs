using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Games;
using iText.Layout.Font;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Electrons.Net8.Models
{
    public class ProfileModel
    {
        public ProfileModel(PlayerProfile profile, IList<HittingStatsRow> hittingStats, IList<PitchingStatsRow> pitchingStats)
        {
            FirstName = profile.FirstName.Trim();
            LastName = profile.LastName.Trim();
            NickName = profile.Nickname;
            BirthDate = profile.DOB;
            HittingStats = hittingStats;
            PitchingStats = pitchingStats;
            Awards = profile.Awards.Select(s => s.AwardData).ToList();
            Hometown = profile.Hometown;
            Height = profile.HeightString;
            Weight = profile.Weight;
            Positions = profile.Positions;
            Bats = profile.Bats.ToString();
            Throws = profile.Throws.ToString();
            Divorces = profile.Divorces.GetValueOrDefault();
            Bitches = profile.HittingStats.Sum(s => s.Bitches);
            ImageFile = profile.ImageFile.TrimStart('/');
            var yearsPlayed = YearsPlayed(profile.HittingStats.ToList(), profile.PitchingStats.ToList());
            YearDisplay = yearsPlayed;
        }

        public string DisplayName => $"{FirstName} {(string.IsNullOrEmpty(NickName) ? "" : $"\"{NickName}\"")} {LastName}";
        [DisplayName("DOB:")]
        public string DOB => BirthDate.HasValue ? BirthDate.Value.ToShortDateString() : "";
        public string AwardString
        {
            get
            {
                if (!Awards.Any())
                    return "";
                var sb = new StringBuilder("<ul>");
                foreach (var award in Awards)
                    sb.AppendFormat("<li>{0}</li>", award);
                sb.Append("</ul>");
                return sb.ToString();
            }
        }
        public string ImageFile { get; set; }
        public IList<HittingStatsRow> HittingStats { get; set; }
        public IList<PitchingStatsRow> PitchingStats { get; set; }
        private string FirstName { get; }
        private string LastName { get; }
        public string FullName => $"{FirstName} {LastName}";
        private string NickName { get; }
        private DateTime? BirthDate { get; }
        private IList<string> Awards { get; }
        [DisplayName("Home Town:")]
        public string Hometown { get; }
        [DisplayName("Height:")]
        public string Height { get; }
        [DisplayName("Weight:")]
        public int? Weight { get; }
        [DisplayName("Positions:")]
        public string Positions { get; }
        [DisplayName("Bats:")]
        public string Bats { get; }
        [DisplayName("Throws:")]
        public string Throws { get; }
        [DisplayName("Divorces:")]
        public int Divorces { get; }
        [DisplayName("Bitches:")]
        public int Bitches { get; }
        [DisplayName("Experience:")]
        public string YearDisplay { get; }

        private string YearsPlayed(List<HittingStats> hittingStats, List<PitchingStats> pitchingStats)
        {
            var hitting_years = hittingStats.Select(s => s.Game.GameDate.Year).Distinct();
            var pitching_years = pitchingStats.Select(s => s.Game.GameDate.Year).Distinct();
            var all_years = hitting_years.Union(pitching_years).Distinct().OrderBy(o => o);
            if (!all_years.Any())
                return "N/A";
            var current_year = DateTime.Now.Year;
            var ranges = GetYearRanges(all_years);

            var formatted_ranges = GetFormattedRanges(ranges);
            var ranges_string = string.Join(", ", formatted_ranges);

            if (all_years.Count() == 1 && all_years.First() == current_year)
                return "Rookie";
            else if (all_years.Count() == 1)
                return $"1 year ({all_years.First()})";
            else
                return $"{all_years.Count()} years ({ranges_string})";
        }

        private IEnumerable<string> GetFormattedRanges(IEnumerable<Tuple<int, int>> ranges)
        {
            var currentYear = DateTime.Now.Year;
            foreach (var range in ranges)
            {
                var start_yr = range.Item1;
                var end_yr = range.Item2;

                if (start_yr == end_yr)
                    yield return start_yr == currentYear ? currentYear.ToString() : start_yr.ToString();
                else
                {
                    var display_end = (end_yr == currentYear) ? "Present" : end_yr.ToString();
                    yield return $"{start_yr}-{display_end}";
                }
            }
        }

        private IEnumerable<Tuple<int, int>> GetYearRanges(IOrderedEnumerable<int> all_years)
        {
            int start = all_years.First();
            int current = 0;
            int prev = all_years.First();
            foreach (var year in all_years)
            {
                current = year;
                if (year == all_years.First())
                    continue;
                if (current != prev + 1)
                {
                    var val= new Tuple<int, int>(start, prev);
                    start = current;
                    prev = current;
                    yield return val;                    
                }
                prev = current;
            }
            yield return new Tuple<int, int>(start, current);
        }
    }
}