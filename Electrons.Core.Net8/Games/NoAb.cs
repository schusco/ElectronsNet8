using System.Linq;

namespace Electrons.Core.Net8.Games
{
    public abstract class NoAb : AtBatResult
    {
        public override int Ab => 0;
    }
    public class Walk : NoAb
    {
        public Walk()
        {
            _result = "walked";
            _eventScoring = "BB";
        }
        public override int Walks => 1;
    }
    public class HitByPitch : NoAb
    {
        public HitByPitch()
        {
            _result = "hit by pitch";
            _eventScoring = "HBP";
        }
        public override int Hbp => 1;
    }
    public class Sacrifice : NoAb
    {
        public Sacrifice()
        {
            _result = "sacrificed";
            _eventScoring = "SAC";
        }
        public override int Outs => _events.Sum(s => s.Outs) + 1;
        public override int Sac => 1;
    }
    public class SacrificeReachOnError : Sacrifice
    {
        public SacrificeReachOnError() : base() { }
        public override int Outs => 0;
    }
    public class SacrificeFly : NoAb
    {
        public SacrificeFly()
        {
            _eventScoring = "SF";
            _result = "hit a sacrifice fly";
        }
        public override int Outs => _events.Sum(s => s.Outs) + 1;
        public override int SacFly => 1;
    }
    public class Interference : NoAb
    {
        public Interference()
        {
            _result = "awarded first on interference";
            _eventScoring = "INT";
        }
        public Player Player { get; private set; }
        public Interference(Player player) => Player = player;
        public override string ToString() => $"awarded first on interference by {Player.LastName}.  ";
    }
}
