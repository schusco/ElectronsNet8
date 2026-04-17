using Electrons.Core.Net8.Entities;
using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Core.Net8.Games
{

    public class PStats : IHasPlayer
    {
        [TableColumn("Pitchers", 1, Class = "alignLeft")]
        public string PitcherDisplay => $"{Player} {(Decision != Decision.ND ? $"({Decision.GetDescription()})" : "")}";
        public string PlayerName => $"{Player.FirstName[0]}. {Player.LastName}";
        public Pitcher Player { get; private set; }
        public Decision Decision { get; private set; }
        public string DecisionDisplay => Decision.GetDescription();
        public int GS { get; private set; }
        [TableColumn("H", 10)]
        public int H { get; private set; }
        [TableColumn("HR", 37)]
        public int HR { get; private set; }
        [TableColumn("BB", 25)]
        public int BB { get; private set; }
        [TableColumn("K", 30)]
        public int K { get; private set; }
        [TableColumn("HB", 35)]
        public int HB { get; private set; }
        [TableColumn("BF", 40)]
        public int BF { get; private set; }
        [TableColumn("R", 15)]
        public int R { get; private set; }
        [TableColumn("ER", 20)]
        public int ER { get; private set; }
        public int Outs { get; private set; }
        [TableColumn("IP", 5)]
        public string IP => $"{Math.Floor(Outs / 3M)}.{Outs % 3}";
        public int CG { get; private set; }
        [TableColumn("ERA", 45)]
        public string EraFormatted => ERA.GetValueOrDefault().ToString("#.00");
        public decimal? ERA => Utilities.CalculateEra((decimal)Outs / 3, ER);
        [TableColumn("WHIP", 50)]
        public string WhipFormatted => WHIP.GetValueOrDefault().ToString("#.00");
        public decimal? WHIP => Utilities.CalculateWhip(BB, H, (decimal)Outs / 3);
        public decimal? K9 => Utilities.CalculateK9(K, (decimal)Outs / 3);
        public decimal? BB9 => Utilities.CalculateBB9(BB, (decimal)Outs / 3);
        public virtual int Pitches { get; private set; }
        public virtual int Balls { get; private set; }
        public virtual int Strikes { get; private set; }
        public int GroundOuts { get; private set; }
        public int FlyOuts { get; private set; }

        Player IHasPlayer.Player => (Player)Player;

        internal Pitcher GetPitcher(IEnumerable<Pitcher> gamePitchers) => gamePitchers.First(s => s == Player);

        internal void UpdateBaserunning(IEnumerable<RunScored> evs)
        {
            R = evs.Count();
            ER = evs.Count(w => w.RunIsEarned);
        }
        internal void SetAsStarter() => GS = 1;
        internal void SetAsCompleteGame() => CG = 1;
        internal void SetDecision(Decision dec) => Decision = dec;
        public override string ToString() => $"{Player} ({Decision})";
        internal static PStats Create(Player pitcher)
        {
            return new PStats
            {
                Player = (Pitcher)pitcher
            };
        }
        internal static PStats Create(IGrouping<Pitcher, AtBat> abs)
        {
            return new PStats
            {
                Player = abs.Key,
                BF = abs.Count(c => c.Result != null),
                H = abs.Sum(s => s?.Result?.Hits ?? 0),
                BB = abs.Sum(s => s?.Result?.Walks ?? 0),
                K = abs.Sum(s => s?.Result?.StrikeOuts ?? 0),
                HR = abs.Sum(s => s?.Result?.HomeRuns ?? 0),
                Outs = abs.Sum(s => s?.Outs ?? 0),
                HB = abs.Sum(s => s?.Result?.Hbp ?? 0),
                Pitches = abs.SelectMany(s => s.Pitches).Count(),
                Balls = abs.Sum(s => s.Balls),
                Strikes = abs.Sum(s => s.Strikes),
                GroundOuts = abs.Count(a => a.Result is GroundOut),
                FlyOuts = abs.Count(a => a.Result is FlyOut)
            };
        }
        public static PStats Create(PitchingStats stats)
        {
            return new PStats()
            {
                BB = stats.Walks,
                BF = stats.BattersFaced,
                CG = stats.CompleteGames,
                Decision = stats.Decision,
                ER = stats.EarnedRuns,
                GS = stats.GameStarted,
                H = stats.Hits,
                HB = stats.HitBatters,
                HR = stats.HomeRuns,
                K = stats.StrikeOuts,
                Player = (Pitcher)stats.Player.Player,
                R = stats.Runs,
                Outs = (int)Math.Round(stats.InningsPitched * 3)
            };
        }

        public void SetPlayerAsDuplicate()
        {
            throw new NotImplementedException();
        }
    }
}
