using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
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
            var yearsPlayed = HittingStats.Select(s => s.Year).Union(PitchingStats.Select(s => s.Year)).Distinct().ToList();
            var years = yearsPlayed.Count;
            var rookie = years == 0 || yearsPlayed.Min() == DateTime.Today.Year;
            YearDisplay = rookie ? "Rookie" : $"{years} year{(years == 1 ? "" : "s")}";
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
    }
}