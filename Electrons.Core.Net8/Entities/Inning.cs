using NHibernate.Mapping.Attributes;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "innings")]
    public class GameInning
    {
        protected GameInning() { }
        private GameInning(GameData game, int i)
        {
            Game = game;
            Inning = i;
        }

        [CompositeId(0), KeyProperty(1, Name = "Inning", Column = "Inning"), KeyManyToOne(2, Name = "Game", Column = "GameId", ClassType = typeof(GameData))]
        public virtual GameData Game { get; protected set; }
        public virtual int Inning { get; protected set; }
        [Property(Column = "Hruns")]
        public virtual int? HomeRuns { get; protected set; }
        [Property(Column = "Aruns")]
        public virtual int? AwayRuns { get; protected set; }
        [Property(Column = "Hhits")]
        public virtual int? HomeHits { get; protected set; }
        [Property(Column = "AHits")]
        public virtual int? AwayHits { get; protected set; }
        [Property(Column = "Herrors")]
        public virtual int? HomeErrors { get; protected set; }
        [Property(Column = "Aerrors")]
        public virtual int? AwayErrors { get; protected set; }
        public override bool Equals(object obj)
        {
            if (!(obj is GameInning))
                return false;
            var inning = (GameInning)obj;
            return inning.Inning == Inning && inning.Game.GameId == Game.GameId;
        }
        public override int GetHashCode()
        {
            return Inning.GetHashCode() * Game.GameId.GetHashCode();
        }
        public override string ToString()
        {
            return $"Inning {Inning} Away: {AwayRuns} Home: {HomeRuns}";
        }
        internal static GameInning CreateNew(GameData game, int i, int? topR, int? botR, int? topH, int? botH, int? topE, int? botE)
        {
            var inning = new GameInning(game, i);
            inning.HomeRuns = botR;
            inning.AwayRuns = topR;
            inning.HomeHits = botH;
            inning.HomeErrors = botE;
            inning.AwayErrors = topE;
            inning.AwayHits = topH;
            return inning;
        }

        protected internal virtual void UpdateRuns(int? topR, int? botR, int? topH, int? botH, int? topE, int? botE)
        {
            HomeRuns = botR;
            AwayRuns = topR;
            HomeHits = botH;
            AwayHits = topH;
            HomeErrors = topE;
            AwayErrors = botE;
        }
    }
}
