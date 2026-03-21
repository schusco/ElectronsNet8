using System.Linq;
using System.Text;

namespace Electrons.Core.Net8.Games
{
    public abstract class Hit : AtBatResult
    {
        public override int Hits => 1;
        public override int Outs => _events.Sum(s => s.Outs);
        public override string EventString(Player batter)
        {
            var sb = new StringBuilder($"{batter.LastName} {_result}");
            if (_fieldLocation != FieldLocation.Undefined)
                sb.Append($" to {_fieldLocation.GetDescription()}.  ");
            else
                sb.Append(".  ");
            AddRunningEventsToString(sb, batter);
            return sb.ToString();
        }
    }
    public class Single : Hit
    {
        public Single()
        {
            _result = "singled";
            _eventScoring = "1B";
        }
        public override string EventString(Player batter)
        {
            var sb = new StringBuilder();
            if (_fieldLocation == FieldLocation.Infield)
            {
                sb.Append($"{batter.LastName} reached on infield single.  ");
                AddRunningEventsToString(sb, batter);
                return sb.ToString();
            }
            return base.EventString(batter);
        }
    }
    public class Double : Hit
    {
        public Double()
        {
            _result = "doubled";
            _eventScoring = "2B";
        }
        public override int Doubles => 1;

    }
    public class Triple : Hit
    {
        public Triple()
        {
            _result = "tripled";
            _eventScoring = "3B";
        }
        public override int Triples => 1;

    }
    public class HomeRun : Hit
    {
        public HomeRun()
        {
            _result = "homered";
            _eventScoring = "HR";
        }
        public override int HomeRuns => 1;

    }
}
