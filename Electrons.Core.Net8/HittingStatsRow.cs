using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Games;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Core.Net8
{
    public interface IDisplayToggleable
    {
        void DisplayAll(bool all);
    }
    public class HittingStatsRow : IHasPlayer, IDisplayToggleable
    {
        public HittingStatsRow() { }
        public HittingStatsRow(bool all) => _displayAll = all;
        public HittingStatsRow(HittingStats s, bool all = false, bool game = false) : this(all)
        {
            if (game)
            {
                FirstName = s.Profile.FirstName;
                LastName = s.Profile.LastName;
            }
            else
            {
                Playoff = s.Game.Playoff;
                Year = s.Game.GameDate.Year;
            }
            Player = s.Profile.Player;
            AtBats = s.AtBats;
            Runs = s.Runs;
            Hits = s.Hits;
            Doubles = s.Doubles;
            Triples = s.Triples;
            HomeRuns = s.HomeRuns;
            Rbis = s.RunsBattedIn;
            Walks = s.Walks;
            StrikeOuts = s.StrikeOuts;
            Hbp = s.HitByPitches;
            StolenBases = s.StolenBases;
            CaughtStealing = s.CaughtStealing;
            SacBunts = s.SacrificeBunts;
            SacFlies = s.SacFlies;
            LeftOnBase = s.LeftOnBase;
            Id = s.Profile.Id;
        }
        public Player Player { get; set; }
        public int Id { get; set; }
        [TableColumn(Optional = true, SortOrder = 1, HeaderText = "Year", FooterProperty = "YearFooter")]
        public string YearDisplay
        {
            get
            {
                if (Year == 0)
                    return null;
                return Playoff ? string.Format("Playoffs '{0}", Year.ToString().Substring(2, 2)) : Year.ToString();
            }
        }
        public int Year { get; set; }
        public string PlayerNameFI => $"{FirstName.Substring(0, 1)}. {LastName}";
        [LinkColumn(NavUrl = "Profile", SortOrder = 2, HeaderText = "Player", Optional = true, FooterProperty = "YearFooter"), LinkParameter("Id", "Id")]
        public string PlayerName => string.IsNullOrEmpty(FirstName) || Year > 0 ? null : string.Format("{0},{1}", LastName, !Player.IsDuplicate ? FirstName.Substring(0, 1) : FirstName.Substring(0, 2));
        [TableColumn(HeaderText = "G", SortOrder = 3, Optional = true, FooterProperty = "SumG")]
        public int? Games { get; set; }
        [TableColumn(HeaderText = "AB", SortOrder = 4, FooterProperty = "SumAb")]
        public int AtBats { get; set; }
        [TableColumn(HeaderText = "R", SortOrder = 5, FooterProperty = "SumR")]
        public int Runs { get; set; }
        [TableColumn(HeaderText = "H", SortOrder = 6, FooterProperty = "SumH")]
        public int Hits { get; set; }
        [TableColumn(HeaderText = "2B", SortOrder = 8, Optional = true, FooterProperty = "Sum2b")]
        public int? Doubles
        {
            get => _displayAll ? _doubles : (int?)null;
            set => _doubles = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "3B", SortOrder = 10, Optional = true, FooterProperty = "Sum3b")]
        public int? Triples
        {
            get => _displayAll ? _triples : (int?)null;
            set => _triples = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "HR", SortOrder = 12, FooterProperty = "SumHr")]
        public int HomeRuns { get; set; }
        [TableColumn(HeaderText = "RBI", SortOrder = 14, FooterProperty = "SumRbi")]
        public int Rbis { get; set; }
        [TableColumn(HeaderText = "BB", SortOrder = 15, Optional = true, FooterProperty = "SumBb")]
        public int? Walks { get; set; }
        [TableColumn(HeaderText = "HBP", SortOrder = 18, Optional = true, FooterProperty = "SumHbp")]
        public int? Hbp
        {
            get => _displayAll ? _hbp : null;
            set => _hbp = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "K", SortOrder = 20, FooterProperty = "SumK")]
        public int StrikeOuts { get; set; }
        [TableColumn(HeaderText = "SB", SortOrder = 22, FooterProperty = "SumSb")]
        public int StolenBases { get; set; }
        [TableColumn(HeaderText = "CS", SortOrder = 25, Optional = true, FooterProperty = "SumCs")]
        public int? CaughtStealing
        {
            get => _displayAll ? _cs : null;
            set => _cs = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "SAC", SortOrder = 28, Optional = true, FooterProperty = "SumSac")]
        public int? SacBunts
        {
            get => _displayAll ? _sac : null;
            set => _sac = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "SF", SortOrder = 30, Optional = true, FooterProperty = "SumSf")]
        public int? SacFlies
        {
            get => _displayAll ? _sf : null;
            set => _sf = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "LOB", SortOrder = 35, Optional = true, FooterProperty = "SumLob")]
        public int? LeftOnBase
        {
            get => _displayAll ? _lob : null;
            set => _lob = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "BA", SortOrder = 40, Format = ".000", FooterProperty = "TotalBa")]
        public decimal? BattingAverage => Utilities.CalculateBa(Hits, AtBats);
        [TableColumn(HeaderText = "SLG", SortOrder = 50, Format = ".000", FooterProperty = "TotalSlg")]
        public decimal? Slugging => Utilities.CalculateSlg(TotalBases, AtBats);
        [TableColumn(HeaderText = "OBP", SortOrder = 45, Format = ".000", FooterProperty = "TotalObp")]
        public decimal? OnBasePct => Utilities.CalculateObp(Hits, Walks.GetValueOrDefault(), _hbp.GetValueOrDefault(), AtBats, _sf.GetValueOrDefault());
        [TableColumn(HeaderText = "OPS", SortOrder = 55, Format = ".000", Optional = true, FooterProperty = "TotalOps")]
        public decimal? Ops => _displayAll ? Utilities.CalculateOps(OnBasePct.GetValueOrDefault(), Slugging.GetValueOrDefault()) : null;
        public int TotalBases => Hits + _doubles + 2 * _triples + 3 * HomeRuns;
        public int SumAb { get; set; }
        public int SumR { get; set; }
        public int SumH { get; set; }
        public int SumHr { get; set; }
        public int SumRbi { get; set; }
        public int SumBb { get; set; }
        public int SumK { get; set; }
        public int SumSb { get; set; }
        public decimal? TotalBa { get; set; }
        public decimal? TotalSlg { get; set; }
        public decimal? TotalObp { get; set; }
        public string YearFooter => "Total";
        public decimal? TotalOps { get; set; }
        public int SumG { get; set; }
        public int? Sum2b { get; set; }
        public int? Sum3b { get; set; }
        public int? SumHbp { get; set; }
        public int? SumCs { get; set; }
        public int? SumSac { get; set; }
        public int? SumSf { get; set; }
        public int? SumLob { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public int UniformNumber { get; set; }
        public bool Playoff { get; set; }
        public void DisplayAll(bool val) => _displayAll = val;
        public static HittingStatsRow Sum(IEnumerable<HittingStats> stats)
        {
            return new HittingStatsRow(true)
            {
                AtBats = stats.Sum(m => m.AtBats),
                CaughtStealing = stats.Sum(m => m.CaughtStealing),
                Doubles = stats.Sum(m => m.Doubles),
                FirstName = stats.First().Profile.FirstName,
                Hbp = stats.Sum(m => m.HitByPitches),
                Hits = stats.Sum(m => m.Hits),
                HomeRuns = stats.Sum(m => m.HomeRuns),
                LastName = stats.First().Profile.LastName,
                LeftOnBase = stats.Sum(m => m.LeftOnBase),
                Playoff = stats.First().Game.Playoff,
                Rbis = stats.Sum(m => m.RunsBattedIn),
                Runs = stats.Sum(m => m.Runs),
                SacBunts = stats.Sum(m => m.SacrificeBunts),
                SacFlies = stats.Sum(m => m.SacFlies),
                StolenBases = stats.Sum(m => m.StolenBases),
                StrikeOuts = stats.Sum(m => m.StrikeOuts),
                Triples = stats.Sum(m => m.Triples),
                Walks = stats.Sum(m => m.Walks),
                Year = stats.First().Game.GameDate.Year,
                Games = stats.Count(),
                Player = stats.First().Profile.Player

            };
        }
        public override string ToString() => $"{LastName}, {FirstName}, Year: {Year}, Playoff: {Playoff}";

        private bool _displayAll = false;
        private int _doubles;
        private int _triples;
        private int? _hbp;
        private int? _cs;
        private int? _sac;
        private int? _sf;
        private int? _lob;

        public static HittingStatsRow Create(HStats hitter, int playerId = 0)
        {
            return new HittingStatsRow(true)
            {
                AtBats = hitter.AB,
                Walks = hitter.BB,
                CaughtStealing = hitter.CS,
                Doubles = hitter.Doubles,
                Triples = hitter.Triples,
                HomeRuns = hitter.HR,
                Rbis = hitter.RBI,
                Runs = hitter.Runs,
                SacBunts = hitter.SAC,
                StolenBases = hitter.SB,
                SacFlies = hitter.SF,
                StrikeOuts = hitter.K,
                Hits = hitter.H,
                Hbp = hitter.HBP,
                Id = playerId,
                LeftOnBase = 0
            };
        }

        public void SetPlayerAsDuplicate()
        {
            Player.SetDuplicate();
        }
    }
}