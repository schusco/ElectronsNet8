using System.Linq;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Games
{
    public class Substitution : InningEvent
    {
        public Substitution() { }
        protected Substitution(Player bench, Player lineup)
        {
            NewPlayer = bench;
            Replaced = lineup;
        }
        public Player NewPlayer { get; private set; }
        public Player Replaced { get; private set; }
        internal static Substitution Create(Player bench, Player lineup) => new Substitution(bench, lineup);
        internal static Substitution PinchHitter(Player bench, Player lineup) => new PinchHitter(bench, lineup);
        internal static Substitution PinchRunner(Player bench, Player lineup) => new PinchRunner(bench, lineup);
        internal static Substitution ReliefPitcher(Player bench, Player lineup) => new ReliefPitcher(bench, lineup);
        internal static Substitution CourtesyRunner(Player bench, Player lineup) => new CourtesyRunner(bench, lineup);
        internal override string EventText => $"{NewPlayer?.LastName} entered the game for {Replaced?.LastName}.  ";
        public override string EventString(Player batter) => ToString();
        public override string ToString() => EventText;
        internal override XElement Xml
        {
            get
            {
                var xml = base.Xml;
                xml.Add(new XElement("NewPlayer", NewPlayer.Xml));
                xml.Add(new XElement("Replaced", Replaced.Xml));
                return xml;
            }
        }
        internal static InningEvent Load(Substitution ev, XElement el)
        {
            ev.NewPlayer = Player.Load(el.Descendants().Single(s => s.Name == "NewPlayer").Descendants().First());
            ev.Replaced = Player.Load(el.Descendants().Single(s => s.Name == "Replaced").Descendants().First());
            return ev;
        }
    }
    public class PinchHitter : Substitution
    {
        public PinchHitter() { }
        internal PinchHitter(Player bench, Player lineup) : base(bench, lineup) { }
        public override string ToString() => $"{NewPlayer?.LastName} batted for {Replaced?.LastName}.  ";


    }
    public class PinchRunner : Substitution
    {
        public PinchRunner() { }
        internal PinchRunner(Player bench, Player lineup) : base(bench, lineup) { }
        public override string ToString() => $"{NewPlayer?.LastName} ran for {Replaced?.LastName}.  ";
    }
    public class CourtesyRunner : PinchRunner
    {
        public CourtesyRunner() { }
        public CourtesyRunner(Player bench, Player lineup) : base(bench, lineup) { }
        public override string EventString(Player batter)
        {
            return $"{NewPlayer.DisplayName} entered as a courtesy runner for {Replaced.DisplayName}.  ";
        }
    }
    public class ReliefPitcher : Substitution
    {
        public ReliefPitcher() { }
        internal ReliefPitcher(Player bench, Player lineup) : base(bench, lineup) { }
        public override string ToString() => $"{NewPlayer?.ToString()} relieved {Replaced?.ToString()}.  ";
    }
}
