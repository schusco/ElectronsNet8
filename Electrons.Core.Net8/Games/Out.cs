using System.Collections.Generic;
using System.Linq;

namespace Electrons.Core.Net8.Games
{
    public abstract class Out : AtBatResult
    {
        public override int Outs => 1;
        public override string ToString() => EventText;
    }
    public class StrikeOut : Out
    {
        public StrikeOut()
        {
            _result = "struck out";
            _eventScoring = "K";
        }
        public override int StrikeOuts => 1;
    }
    public class StrikeOutSwinging : StrikeOut
    {
        public StrikeOutSwinging() : base()
        {
            _result = "struck out swinging";
        }
    }
    public class StrikeOutLooking : StrikeOut
    {
        public StrikeOutLooking() : base()
        {
            _result = "struck out looking";
        }
    }
    public class DropThirdStrike : StrikeOut
    {
        public DropThirdStrike()
        {
            _result = "reached on drop third stike.";
            _eventScoring = "K/WP";
        }
        public override int Outs => 0;
    }
    public class FlyOut : Out
    {
        public FlyOut()
        {
            _result = "flied out";
            _eventScoring = "F";
        }
        public override int Outs => 1 + Events.Sum(s => s.Outs);
    }
    public class ReachedOnError : Out
    {
        public ReachedOnError()
        {
            _result = "reached on an error";
            _eventScoring = "E";
        }
        public override int Outs => Events.Sum(s => s.Outs);
        public override int Errors => 1;
    }
    public class FieldersChoice : GroundOut
    {
        public override int Outs => Events.Sum(s => s.Outs);
        public FieldersChoice()
        {
            _result = "reached on fielder's choice";
            _eventScoring = "FC";
            _scoringIsRequired = true;
        }
        public void AddAdvances(IEnumerable<RunningEvent> revs)
        {
            if (AdvancesAdded)
                return;
            foreach (var ev in revs)
                AddEvent(ev);
            AdvancesAdded = true;
        }
        public bool AdvancesAdded { get; private set; }
    }
    public class GroundOut : Out
    {
        public GroundOut() => _result = "grounded out";
    }
    public class DoublePlay : GroundOut
    {
        public DoublePlay() => _result = "hit into a double play";
        public override int Outs => 2;
    }
    public class TriplePlay : GroundOut
    {
        public TriplePlay() => _result = "hit into a triple play";
        public override int Outs => 3;
    }
    public class LinedIntoDoublePlay : FlyOut
    {
        public LinedIntoDoublePlay() => _result = "lined into a double play";
        public override int Outs => 2;
    }
    public class LinedIntoTriplePlay : FlyOut
    {
        public LinedIntoTriplePlay() => _result = "lined into a triple play";
        public override int Outs => 3;
    }
}
