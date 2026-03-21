using NHibernate.Mapping.Attributes;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "hittingstats")]
    public class HittingStats
    {
        public HittingStats() { }
        protected HittingStats(GameData game, PlayerProfile player)
        {
            Game = game;
            Profile = player;
        }

        [Id(Name = "Id", Column = "ID"), Generator(Class = "native")]
        public virtual int Id { get; protected set; }
        [ManyToOne(Column = "Player_ID", ClassType = typeof(PlayerProfile))]
        public virtual PlayerProfile Profile { get; protected set; }
        [ManyToOne(Column = "Game_ID", ClassType = typeof(GameData))]
        public virtual GameData Game { get; protected set; }
        [Property(Column = "AB")]
        public virtual int AtBats { get; protected set; }
        [Property(Column = "R")]
        public virtual int Runs { get; protected set; }
        [Property(Column = "H")]
        public virtual int Hits { get; protected set; }
        [Property(Column = "2B")]
        public virtual int Doubles { get; protected set; }
        [Property(Column = "3B")]
        public virtual int Triples { get; protected set; }
        [Property(Column = "HR")]
        public virtual int HomeRuns { get; protected set; }
        [Property(Column = "RBI")]
        public virtual int RunsBattedIn { get; protected set; }
        [Property(Column = "BB")]
        public virtual int Walks { get; protected set; }
        [Property(Column = "HBP")]
        public virtual int HitByPitches { get; protected set; }
        [Property(Column = "K")]
        public virtual int StrikeOuts { get; protected set; }
        [Property(Column = "SB")]
        public virtual int StolenBases { get; protected set; }
        [Property(Column = "CS")]
        public virtual int CaughtStealing { get; protected set; }
        [Property(Column = "SAC")]
        public virtual int SacrificeBunts { get; protected set; }
        [Property(Column = "SF")]
        public virtual int SacFlies { get; protected set; }
        [Property(Column = "LOB")]
        public virtual int LeftOnBase { get; protected set; }
        [Property(Column = "Bitching")]
        public virtual int Bitches { get; protected set; }
        [Property(Column = "PO")]
        public virtual int PutOuts { get; protected set; }
        [Property(Column = "A")]
        public virtual int Assists { get; protected set; }
        [Property(Column = "E")]
        public virtual int Errors { get; protected set; }
        public override string ToString()
        {
            return $"{Profile.LastName}, {Game.GameDate.ToShortDateString()} vs. {Game.Opponent}";
        }

        internal static HittingStats CreateNew(GameData game, PlayerProfile player)
        {
            return new HittingStats(game, player);
        }

        protected internal virtual void Update(HittingStatsRow stats)
        {
            AtBats = stats.AtBats;
            Runs = stats.Runs;
            Hits = stats.Hits;
            Doubles = stats.Doubles.GetValueOrDefault();
            Triples = stats.Triples.GetValueOrDefault();
            HomeRuns = stats.HomeRuns;
            RunsBattedIn = stats.Rbis;
            Walks = stats.Walks.GetValueOrDefault();
            HitByPitches = stats.Hbp.GetValueOrDefault();
            StrikeOuts = stats.StrikeOuts;
            StolenBases = stats.StolenBases;
            CaughtStealing = stats.CaughtStealing.GetValueOrDefault();
            SacrificeBunts = stats.SacBunts.GetValueOrDefault();
            SacFlies = stats.SacFlies.GetValueOrDefault();
            LeftOnBase = stats.LeftOnBase.GetValueOrDefault();

        }
    }
}
