using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Games;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Core.Net8
{
    public class PitchingStatsRow
    {
        public PitchingStatsRow() { }
        public PitchingStatsRow(bool all) => _displayAll = all;
        public PitchingStatsRow(PitchingStats obj, bool all = false, bool game = false, bool appearances = true) : this(all)
        {
            _appearances = appearances;
            if (!game)
            {
                Year = obj.Game.GameDate.Year;
                Playoff = obj.Game.Playoff;
            }
            else
            {
                FirstName = obj.Player?.FirstName;
                LastName = obj.Player?.LastName;
            }
            Starts = obj.GameStarted;
            CompleteGames = obj.CompleteGames;
            Innings = obj.InningsPitched;
            Hits = obj.Hits;
            EarnedRuns = obj.EarnedRuns;
            Walks = obj.Walks;
            Runs = obj.Runs;
            StrikeOuts = obj.StrikeOuts;
            HitBatters = obj.HitBatters;
            HomeRuns = obj.HomeRuns;
            BattersFaced = obj.BattersFaced;
            Id = obj.Player?.Id ?? 0;
            Decision = obj.DecisionVal;
        }

        public int Id { get; set; }
        public int Year { get; set; }
        [TableColumn(Optional = true, HeaderText = "Year", FooterProperty = "Total")]
        public string YearDisplay
        {
            get
            {
                if (Year == 0)
                    return null;
                return Playoff ? $"Playoffs '{Year.ToString().Substring(2, 2)}" : Year.ToString();
            }
        }
        [LinkColumn(NavUrl = "Profile", FromRoot = true, HeaderText = "Player", Optional = true, SortOrder = 1, FooterProperty = "Total"), LinkParameter("Id", "Id")]
        public string Player => string.IsNullOrEmpty(FirstName) || Year > 0 ? null : string.Format("{0},{1}", LastName, (FirstName ?? "").Substring(0, 1));

        internal static PitchingStatsRow Sum(IEnumerable<PitchingStats> statss)
        {
            var stats = statss.ToList();
            return new PitchingStatsRow
            {
                BattersFaced = stats.Sum(m => m.BattersFaced),
                CompleteGames = stats.Sum(m => m.CompleteGames),
                EarnedRuns = stats.Sum(m => m.EarnedRuns),
                FirstName = stats.First().Player.FirstName,
                Games = stats.Count(),
                HitBatters = stats.Sum(m => m.HitBatters),
                Hits = stats.Sum(m => m.Hits),
                HomeRuns = stats.Sum(m => m.HomeRuns),
                Innings = Math.Round(stats.Sum(m => m.InningsPitched), 2),
                LastName = stats.First().Player.LastName,
                Losses = stats.Count(m => m.Decision == Net8.Decision.L),
                Playoff = stats.First().Game.Playoff,
                Runs = stats.Sum(m => m.Runs),
                SaveOpportunities = stats.Count(m => m.Decision.IsIn(Net8.Decision.S, Net8.Decision.BSW, Net8.Decision.BSL, Net8.Decision.BS)),
                Saves = stats.Count(m => m.Decision == Net8.Decision.S),
                Starts = stats.Sum(m => m.GameStarted),
                StrikeOuts = stats.Sum(m => m.StrikeOuts),
                Walks = stats.Sum(m => m.Walks),
                Wins = stats.Count(m => m.Decision == Net8.Decision.W),
                Year = stats.First().Game.GameDate.Year
            };
        }

        [TableColumn(HeaderText = "W", Optional = true, SortOrder = 3, FooterProperty = "SumW")]
        public int? Wins { get; set; }
        [TableColumn(HeaderText = "L", Optional = true, SortOrder = 4, FooterProperty = "SumL")]
        public int? Losses { get; set; }
        [TableColumn(HeaderText = "S", Optional = true, SortOrder = 6, FooterProperty = "SumS")]
        public int? Saves { get; set; }
        [TableColumn(HeaderText = "SVO", Optional = true, SortOrder = 7, FooterProperty = "SumSvo")]
        public int? SaveOpportunities
        {
            get => _displayAll ? _svo : null;
            set => _svo = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "G", Optional = true, SortOrder = 10, FooterProperty = "SumG")]
        public int? Games
        {
            get => _displayAll && _appearances ? _games : (int?)null;
            set => _games = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "GS", SortOrder = 15, FooterProperty = "SumGs")]
        public int? Starts { get; set; }
        [TableColumn(HeaderText = "IP", Format = "0.0", SortOrder = 20, FooterProperty = "SumIp")]
        public decimal DisplayInnings
        {
            get
            {
                var totalOuts = Math.Round(Innings * 3);
                return decimal.Parse($"{Math.Floor(totalOuts / 3)}.{totalOuts % 3}");
            }
        }
        public decimal Innings { get; set; }
        private int Outs => Convert.ToInt32(Math.Round(Innings * 3, 0));
        private decimal DecimalInnings => Outs / 3M;
        [TableColumn("H", FooterProperty = "SumH", SortOrder = 25)]
        public int? Hits { get; set; }
        [TableColumn("R", Optional = true, FooterProperty = "SumR", SortOrder = 30)]
        public int? Runs
        {
            get => _displayAll ? _runs : null;
            set => _runs = value.GetValueOrDefault();
        }
        [TableColumn("ER", Optional = true, FooterProperty = "SumEr", SortOrder = 35)]
        public int? EarnedRuns { get; set; }
        [TableColumn("BB", Optional = true, FooterProperty = "SumBb", SortOrder = 40)]
        public int? Walks { get; set; }
        [TableColumn("K", Optional = true, FooterProperty = "SumK", SortOrder = 45)]
        public int? StrikeOuts { get; set; }
        [TableColumn("HB", Optional = true, FooterProperty = "SumHb", SortOrder = 50)]
        public int? HitBatters
        {
            get => _displayAll ? _hb : null;
            set => _hb = value.GetValueOrDefault();
        }
        [TableColumn("HR", Optional = true, FooterProperty = "SumHr", SortOrder = 55)]
        public int? HomeRuns
        {
            get => _displayAll ? _hr : null;
            set => _hr = value.GetValueOrDefault();
        }
        [TableColumn("BF", Optional = true, FooterProperty = "SumBf", SortOrder = 60)]
        public int? BattersFaced
        {
            get => _displayAll ? _bf : null;
            set => _bf = value.GetValueOrDefault();
        }
        [TableColumn(HeaderText = "CG", Optional = true, FooterProperty = "SumCg", SortOrder = 65)]
        public int? CompleteGames
        {
            get => _displayAll ? _cg : null;
            set => _cg = value.GetValueOrDefault();
        }
        [TableColumn("ERA", Optional = true, Format = "0.00", FooterProperty = "TotalEra", SortOrder = 75)]
        public decimal? EarnedRunAvg => Utilities.CalculateEra(Outs / 3M, EarnedRuns ?? 0);
        [TableColumn("WHIP", Optional = true, Format = "0.00", FooterProperty = "TotalWhip", SortOrder = 85)]
        public decimal? Whip => _displayAll ? Utilities.CalculateWhip(Walks ?? 0, Hits ?? 0, DecimalInnings) : null;
        [TableColumn("K/9", Optional = true, Format = "0.00", FooterProperty = "TotalK9", SortOrder = 95)]
        public decimal? StrikeOutsPer9 => _displayAll ? Utilities.CalculateK9(StrikeOuts ?? 0, DecimalInnings) : null;
        [TableColumn("BB/9", Optional = true, Format = "0.00", FooterProperty = "TotalBB9", SortOrder = 100)]
        public decimal? WalksPer9 => _displayAll ? Utilities.CalculateBB9(Walks ?? 0, DecimalInnings) : null;
        public int? SumW { get; set; }
        public int? SumL { get; set; }
        public int? SumS { get; set; }
        public int? SumSvo { get; set; }
        public int? SumG { get; set; }
        public int SumGs { get; set; }
        public decimal SumIp { get; set; }
        public int? SumH { get; set; }
        public int? SumR { get; set; }
        public int? SumEr { get; set; }
        public int? SumBb { get; set; }
        public int? SumK { get; set; }
        public int? SumHb { get; set; }
        public int? SumHr { get; set; }
        public int? SumBf { get; set; }
        public int? SumCg { get; set; }
        public decimal? TotalEra { get; set; }
        public decimal? TotalWhip { get; set; }
        public decimal? TotalK9 { get; set; }
        public decimal? TotalBB9 { get; set; }
        public string Total => "Total";

        [TableColumn("Dec.", Optional = true, SortOrder = 5)]
        public string Decision { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Playoff { get; set; }
        public void DisplayAll(bool val)
        {
            _displayAll = val;
            _appearances = val;
        }
        public override string ToString()
        {
            return $"{LastName}, {FirstName}, Year: {Year}, Playoff: {Playoff}";
        }
        private bool _displayAll;
        private bool _appearances;
        private int _games;
        private int? _svo;
        private int? _runs;
        private int? _hb;
        private int? _hr;
        private int? _bf;
        private int? _cg;

        public static PitchingStatsRow Create(PStats pitcher, int pitcherId)
        {
            return new PitchingStatsRow(true)
            {
                Id = pitcherId,
                Starts = pitcher.GS,
                EarnedRuns = pitcher.ER,
                Runs = pitcher.R,
                Hits = pitcher.H,
                StrikeOuts = pitcher.K,
                Walks = pitcher.BB,
                BattersFaced = pitcher.BF,
                CompleteGames = pitcher.CG,
                HitBatters = pitcher.HB,
                HomeRuns = pitcher.HR,
                Innings = (decimal)pitcher.Outs / 3,
            };
        }
    }
}