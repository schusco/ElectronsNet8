using NHibernate.Mapping.Attributes;
using System.Linq;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "rhe"), Discriminator(Column = "HV")]
    public class LineScore
    {
        protected LineScore() { }
        protected LineScore(GameData game, string hv)
        {
            Game = game;
            HV = hv;
            FirstInning = "0";
            SecondInning = "0";
            ThirdInning = "0";
            FourthInning = "0";
            FifthInning = "0";
            SixthInning = "0";
            SeventhInning = "0";
        }
        [Id(Column = "ID", Name = "Id"), Generator(Class = "native")]
        public virtual int Id { get; protected set; }
        //[CompositeId(0), KeyProperty(1, Name = "HV", Column = "HV"), KeyManyToOne(2, Name = "Game", Column = "Game_ID", ClassType = typeof(GameData))]
        public virtual string HV { get; protected set; }
        [ManyToOne(Column = "Game_ID", ClassType = typeof(GameData))]
        public virtual GameData Game { get; protected set; }
        [Property(Column = "_1")]
        public virtual string FirstInning { get; protected set; }
        [Property(Column = "_2")]
        public virtual string SecondInning { get; protected set; }
        [Property(Column = "_3")]
        public virtual string ThirdInning { get; protected set; }
        [Property(Column = "_4")]
        public virtual string FourthInning { get; protected set; }
        [Property(Column = "_5")]
        public virtual string FifthInning { get; protected set; }
        [Property(Column = "_6")]
        public virtual string SixthInning { get; protected set; }
        [Property(Column = "_7")]
        public virtual string SeventhInning { get; protected set; }
        [Property(Column = "R")]
        public virtual int Runs { get; protected set; }
        [Property(Column = "H")]
        public virtual int Hits { get; protected set; }
        [Property(Column = "E")]
        public virtual int Errors { get; protected set; }
        public virtual LineScoreModel GetModel()
        {
            var model = new LineScoreModel();
            model.Hits = Hits.ToString();
            model.Errors = Errors.ToString();
            model.Runs = Runs.ToString();
            model.Innings.Add(FirstInning.ToString());
            model.Innings.Add(SecondInning.ToString());
            model.Innings.Add(ThirdInning.ToString());
            model.Innings.Add(FourthInning.ToString());
            model.Innings.Add(FifthInning.ToString());
            model.Innings.Add(SixthInning.ToString());
            model.Innings.Add(SeventhInning.ToString());
            return model;
        }

        //public override bool Equals(object obj)
        //{
        //    if (!(obj is LineScore))
        //        return false;
        //    var ls = (LineScore)obj;
        //    return ls.Game.GameId == Game.GameId && ls.HV == HV;
        //}
        //public override int GetHashCode()
        //{
        //    return Game.GameId.GetHashCode() * HV.GetHashCode();
        //}

        protected internal virtual void SetInningValue(object inning, object runs)
        {
            int val;
            var isInning = int.TryParse(inning.ToString(), out val);
            var startChar = isInning ? "_" : "";
            var propName = $"{startChar}{inning}";
            var props = GetType().GetProperties().Where(w => w.GetCustomAttributes(false).Any(a => a is PropertyAttribute));
            foreach (var prop in props)
            {
                var attr = (PropertyAttribute)prop.GetCustomAttributes(false).First();
                if (attr.Column == propName)
                    prop.SetValue(this, isInning ? runs : int.Parse(runs.ToString()));
            }
        }

        protected internal virtual int? GetInningValue(int inning)
        {
            int returnVal;
            var propName = $"_{inning}";
            var props = GetType().GetProperties().Where(w => w.GetCustomAttributes(false).Any(a => a is PropertyAttribute));
            foreach (var prop in props)
            {
                var attr = (PropertyAttribute)prop.GetCustomAttributes(false).First();
                if (attr.Column == propName)
                {
                    string objVal = prop.GetValue(this)?.ToString();
                    if (int.TryParse(objVal, out returnVal))
                        return returnVal;
                }
            }
            return null;
        }
    }

    [Subclass(ExtendsType = typeof(LineScore), DiscriminatorValue = "H")]
    public class HomeScore : LineScore
    {
        protected HomeScore() { }
        protected HomeScore(GameData game, string hv) : base(game, hv) { }
        internal static HomeScore CreateNew(GameData game)
        {
            return new HomeScore(game, "H");
        }
        public override LineScoreModel GetModel()
        {
            var model = base.GetModel();
            //model.Team = Game.HV == Core.HV.H ? "Electrons" : Game.Opponent;
            //foreach (var inning in Game.ExtraInnings.OrderBy(o => o.InningNumber))
            //    model.Innings.Add(inning.BottomRuns.ToString());
            return model;
        }
    }

    [Subclass(ExtendsType = typeof(LineScore), DiscriminatorValue = "V")]
    public class AwayScore : LineScore
    {
        protected AwayScore() { }
        protected AwayScore(GameData game, string hv) : base(game, hv) { }
        internal static AwayScore CreateNew(GameData game)
        {
            return new AwayScore(game, "V");
        }
        public override LineScoreModel GetModel()
        {
            var model = base.GetModel();
            //model.Team = Game.HV == Core.HV.V ? "Electrons" : Game.Opponent;
            //foreach (var inning in Game.ExtraInnings.OrderBy(o => o.InningNumber))
            //    model.Innings.Add(inning.TopRuns.ToString());
            return model;
        }
    }
}
