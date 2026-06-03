using Electrons.Core.Net8.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;


namespace Electrons.Core.Net8.Games
{
    public interface IHasPlayer
    {
        Player Player { get; }
        void SetPlayerAsDuplicate();
    }
    public class HStats : IHasPlayer
    {
        private HStats() { }
        [TableColumn("Hitters", 1, Class = "alignLeft")]
        public string PlayerDisplay => $"{Player.LastName}, {Player.FirstName[0]}";
        public string PlayerName => $"{Player.FirstName[0]}. {Player.LastName}";
        public Player Player { get; private set; }
        public int G { get; private set; }
        [TableColumn("2B", 25)]
        public int Doubles { get; set; }
        [TableColumn("3B", 30)]
        public int Triples { get; set; }
        [TableColumn("HR", 35)]
        public int HR { get; set; }
        [TableColumn("AB", 5)]
        public int AB { get; set; }
        [TableColumn("R", 10)]
        public int Runs { get; private set; }
        [TableColumn("H", 15)]
        public int H { get; set; }
        [TableColumn("BB", 40)]
        public int BB { get; private set; }
        [TableColumn("K", 45)]
        public int K { get; private set; }
        [TableColumn("SB", 50)]
        public int SB { get; private set; }
        [TableColumn("CS", 55)]
        public int CS { get; set; }
        [TableColumn("RBI", 20)]
        public int RBI { get; set; }
        [TableColumn("HBP", 48)]
        public int HBP { get; private set; }
        public int SAC { get; private set; }
        public int SF { get; private set; }
        [TableColumn("AVG", 60, Class = "alignRight")]
        public string BaFormatted => BA.GetValueOrDefault().ToString("#.000");
        public decimal? BA => Utilities.CalculateBa(H, AB);
        [TableColumn("SLG", 70, Class = "alignRight")]
        public string SlgFormatted => SLG.GetValueOrDefault().ToString("#.000");
        public decimal? SLG => Utilities.CalculateSlg(TotalBases, AB);
        private decimal TotalBases => Utilities.CalculateTB(H, Doubles, Triples, HR);
        [TableColumn("OBP", 65, Class = "alignRight")]
        public string ObpFormatted => OBP.GetValueOrDefault().ToString("#.000");
        public decimal? OBP => Utilities.CalculateObp(H, BB, HBP, AB, SF);
        [TableColumn("OPS", 75, Class = "alignRight")]
        public string OpsFormatted => OPS.GetValueOrDefault().ToString("#.000");
        public decimal? OPS => Utilities.CalculateOps(OBP.GetValueOrDefault(), SLG.GetValueOrDefault());
        public override string ToString() => $"{Player.LastName}: {BA?.ToString("f3")}/{OBP?.ToString("f3")}/{SLG?.ToString("f3")}";
        internal void UpdateFromAbList(List<AtBat> abs)
        {
            AB = abs.Sum(s => s?.Result?.Ab ?? 0);
            H = abs.Sum(s => s?.Result?.Hits ?? 0);
            Doubles = abs.Sum(s => s?.Result?.Doubles ?? 0);
            Triples = abs.Sum(s => s?.Result?.Triples ?? 0);
            BB = abs.Sum(s => s?.Result?.Walks ?? 0);
            HR = abs.Sum(s => s?.Result?.HomeRuns ?? 0);
            RBI = abs.Sum(s => s?.RunsBattedIn ?? 0);
            HBP = abs.Sum(s => s?.Result?.Hbp ?? 0);
            SAC = abs.Sum(s => s?.Result?.Sac ?? 0);
            SF = abs.Sum(s => s?.Result?.SacFly ?? 0);
            K = abs.Sum(s => s?.Result?.StrikeOuts ?? 0);
        }
        internal static HStats Sum(IEnumerable<HStats> input)
        {
            return new HStats
            {
                BB = input.Sum(m => m.BB),
                G = input.Count(),
                Doubles = input.Sum(m => m.Doubles),
                AB = input.Sum(m => m.AB),
                H = input.Sum(m => m.H),
                Triples = input.Sum(m => m.Triples),
                CS = input.Sum(m => m.CS),
                HBP = input.Sum(m => m.HBP),
                HR = input.Sum(m => m.HR),
                K = input.Sum(m => m.K),
                RBI = input.Sum(m => m.RBI),
                Runs = input.Sum(m => m.Runs),
                SAC = input.Sum(m => m.SAC),
                SB = input.Sum(m => m.SB),
                SF = input.Sum(m => m.SF),
                Player = input.First().Player
            };
        }

        internal static HStats Create(IGrouping<Player, AtBat> abs)
        {
            var stats= new HStats
            {
                Player = abs.Key                
            };
            stats.UpdateFromAbList(abs.ToList());
            return stats;
        }
        internal static HStats Create(HittingStatsRow hitter)
        {
            return new HStats
            {
                AB = hitter.AtBats,
                BB = hitter.Walks.GetValueOrDefault(),
                CS = hitter.CaughtStealing.GetValueOrDefault(),
                Doubles = hitter.Doubles.GetValueOrDefault(),
                Triples = hitter.Triples.GetValueOrDefault(),
                HR = hitter.HomeRuns,
                RBI = hitter.Rbis,
                Runs = hitter.Runs,
                SAC = hitter.SacBunts.GetValueOrDefault(),
                SB = hitter.StolenBases,
                SF = hitter.SacFlies.GetValueOrDefault(),
                K = hitter.StrikeOuts,
                H = hitter.Hits,
                HBP = hitter.Hbp.GetValueOrDefault(),
                G = 1,
                Player = Player.Create(hitter.UniformNumber, hitter.LastName, hitter.FirstName, hitter.Id),
            };
        }
        public static HStats Create(Player player)
        {
            return new HStats
            {
                Player = player
            };
        }

        internal void UpdateBaserunning(IEnumerable<RunningEvent> evs, Player player)
        {
            SB = evs.Sum(s => s.StolenBases);
            CS = evs.Sum(s => s.CaughtStealing);
            Runs = evs.Count(w => w is RunScored);
        }

        public void SetPlayerAsDuplicate()
        {
            Player.SetDuplicate();
        }

        public static HStats Create(HittingStats stats)
        {
            return new HStats
            {
                AB = stats.AtBats,
                BB = stats.Walks,
                CS = stats.CaughtStealing,
                Doubles = stats.Doubles,
                H = stats.Hits,
                HBP = stats.HitByPitches,
                HR = stats.HomeRuns,
                K = stats.StrikeOuts,
                Player = stats.Profile.Player,
                RBI = stats.RunsBattedIn,
                Runs = stats.Runs,
                SAC = stats.SacrificeBunts,
                SB = stats.StolenBases,
                SF = stats.SacFlies,
                Triples = stats.Triples
            };
        }
        
    }
}
