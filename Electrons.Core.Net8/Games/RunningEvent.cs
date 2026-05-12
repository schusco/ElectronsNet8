using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Games
{
    public abstract class RunningEvent : InningEvent
    {
        protected RunningEvent() { }
        protected RunningEvent(BaseRunner player, OnBase nextBase) => AdvanceRunner(player, nextBase);
        public virtual int CaughtStealing => 0;
        public virtual int StolenBases => 0;
        public Player Player { get; protected set; }
        public Player OriginalRunner { get; protected set; }
        public Player ResponsiblePitcher { get; protected set; }
        public OnBase AdvanceTo { get; protected set; }
        public OnBase? OutAt { get; protected set; }
        public bool ReachedOnError { get; private set; }
        public virtual void AdvanceRunner(BaseRunner runner, OnBase nextBase)
        {
            Player = runner.Runner ?? throw new BaseballGameException($"No runner specified");
            ResponsiblePitcher = runner.ResponsiblePitcher;
            AdvanceTo = nextBase;
            ReachedOnError = runner.ReachedOnError;
            OriginalRunner = runner.OriginalRunner;
        }
        internal void SetPlayer(Player player) => Player = player;
        internal void SetPitcher(Player pitcher) => ResponsiblePitcher = pitcher;
        internal void BackOne(AtBat ab)
        {
            var adv = (int)AdvanceTo;
            if (adv == 0)
                adv = 4;
            else
                adv = (int)Math.Floor(adv / 2M);

            AdvanceTo = (OnBase)adv;
            ab.OnRunningEventChanged(this);
        }
        internal override string EventText => $"{Player.DisplayName} to {AdvanceTo.GetDescription().ToLower()}.  ";
        public override string EventString(Player batter) => ToString();
        public override string ToString() => EventText;
        internal override XElement Xml
        {
            get
            {
                var xml = base.Xml;
                xml.Add(new XElement("Runner", Player?.Xml));
                if (!(ResponsiblePitcher is null))
                    xml.Add(new XElement("ResponsiblePitcher", ResponsiblePitcher.Xml));
                if (!(OriginalRunner is null))
                    xml.Add(new XElement("OriginalRunner", OriginalRunner.Xml));
                xml.Add(new XElement("AdvanceTo", AdvanceTo.ToString()));
                return xml;
            }
        }
        internal static InningEvent Load(RunningEvent ev, XElement el)
        {
            var obs = el.Descendants().Where(s => s.Name == "AdvanceTo" && s.Parent.Name == ev.GetType().Name);
            var ob = (OnBase)Enum.Parse(typeof(OnBase), el.Descendants().Single(s => s.Name == "AdvanceTo" && s.Parent.Name == ev.GetType().Name).Value);
            var outAtEl = el.Descendants().SingleOrDefault(s => s.Name == "OutAt");
            if (outAtEl != null)
            {
                var outAt = (OnBase)Enum.Parse(typeof(OnBase), outAtEl.Value);
                ev.OutAt = outAt;
            }
            Player player = null;
            var playerEl = el.Descendants().Single(s => s.Name == "Runner" && s.Parent.Name == ev.GetType().Name).Descendants().FirstOrDefault();
            if (!(playerEl is null))
                player = Player.Load(playerEl);
            var pitcherEl = el.Descendants().SingleOrDefault(s => s.Name == "ResponsiblePitcher" && s.Parent.Name == ev.GetType().Name);
            Player pitcher = null;
            if (pitcherEl != null)
                pitcher = Player.Load(pitcherEl.Descendants().Single(s => s.Name == "Player"));
            var advBase = ev.OutAt != null ? OnBase.None : ob;
            var originalRunnerEl = el.Descendants().FirstOrDefault(s => s.Name == "OriginalRunner" && s.Parent.Name == ev.GetType().Name);
            Player original = null;
            if (originalRunnerEl != null)
                original = Player.Load(originalRunnerEl.Descendants().First(s => s.Name == "Player"));
            if (!(player is null))
                ev.AdvanceRunner(BaseRunner.Create(player, pitcher, original), advBase);
            return ev;
        }
    }
    public class RunnerAdvance : RunningEvent
    {
        public RunnerAdvance() { }

        protected RunnerAdvance(BaseRunner player, OnBase nextBase) : base(player, nextBase) { }

        internal static InningEvent Create(BaseRunner runner, OnBase tobase, AdvanceReason reason)
        {
            switch (reason)
            {
                case AdvanceReason.Ab:
                    return new RunnerAdvanceFromAb(runner, tobase);
                case AdvanceReason.Throw:
                    return new RunnerAdvanceOnThrow(runner, tobase);
                case AdvanceReason.Error:
                    return new AdvancedOnError(runner, tobase);
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }
            throw new NotImplementedException();
        }
    }
    public class RunnerAdvanceFromAb : RunnerAdvance
    {
        public RunnerAdvanceFromAb() { }
        public RunnerAdvanceFromAb(BaseRunner player, OnBase next) : base(player, next) { }
    }
    public class RunnerAdvanceOnThrow : RunnerAdvance
    {
        public RunnerAdvanceOnThrow() { }
        public RunnerAdvanceOnThrow(BaseRunner player, OnBase next) : base(player, next) { }
        internal override string EventText => $"{Player.DisplayName} advanced to {AdvanceTo.GetDescription().ToLower()} on throw.  ";
    }
    public class AdvancedOnError : RunnerAdvance
    {
        public AdvancedOnError()
        {
            _eventScoring = "E";
            _scoringIsRequired = true;
        }
        public AdvancedOnError(BaseRunner player, OnBase next) : base(player, next)
        {
            _eventScoring = "E";
            _scoringIsRequired = true;
        }
        public override int Errors => 1;
        public override string ToString()
        {
            var sb = new StringBuilder($"{Player.DisplayName} advanced to {AdvanceTo.ToString().ToLower()} on error");
            sb.Append($"{(HasFielders ? $" by {AllPositions[FieldingPlayer].PositionText}.  " : ".  ")}");
            foreach (var ev in Events)
            {
                if (ev is RunningEvent rev && Events.Any(a => a.Sequence > ev.Sequence && a is RunningEvent chRev && chRev.Player == rev.Player))
                    continue;
                sb.Append(ev.ToString().Trim().TrimEnd('.'));
                if (!(ev == Events.Last()))
                    sb.Append(", ");
            }
            if (Events.Any())
                sb.Append(" on same error.  ");
            return sb.ToString();
        }
    }
    public class RunScored : RunnerAdvance
    {
        public RunScored() { }
        public RunScored(BaseRunner runner) : base(runner, OnBase.None) { }
        public override int Runs => 1;
        public virtual bool RunIsEarned => true;
        internal override string EventText => $"{Player.DisplayName} scored.  ";
        public override string ToString() => EventText;
        public static RunningEvent Unearned(RunningEvent advance)
        {
            return new UnearnedRunScored() { Player = advance.Player, ResponsiblePitcher = advance.ResponsiblePitcher, Sequence = advance.Sequence };
        }
    }
    public enum AdvanceReason
    {
        Error,
        Ab,
        Throw
    }
    public class UnearnedRunScored : RunScored
    {
        public UnearnedRunScored() { }
        public UnearnedRunScored(BaseRunner runner) : base(runner) { }
        public override bool RunIsEarned => false;
    }
    public class RunScoredOnError : UnearnedRunScored
    {
        public RunScoredOnError()
        {
            _eventScoring = "E";
            _scoringIsRequired = true;
        }
        public RunScoredOnError(BaseRunner runner) : base(runner)
        {
            _eventScoring = "E";
            _scoringIsRequired = true;
        }
        public override int Runs => 1 + Events?.Sum(s => s.Runs) ?? 0;
        public override int Errors => 1;
        internal string ErrorString => $"on error{(HasFielders ? $" by {AllPositions[FieldingPlayer].PositionText}.  " : ".  ")}";
        public override string EventString(Player batter)
        {
            var sb = new StringBuilder($"{Player?.DisplayName} scored {ErrorString}");
            foreach (var ev in Events)
            {
                sb.Append(ev.ToString().Trim().TrimEnd('.'));
                if (!(ev == Events.Last()))
                    sb.Append(", ");
            }
            if (Events.Any())
                sb.Append(" on same error.  ");
            return sb.ToString();
        }
        public override string ToString() => EventString(Player);
    }
    public class StolenBase : RunningEvent
    {
        public StolenBase()
        {
            _eventScoring = "SB";
        }
        public StolenBase(BaseRunner player, OnBase next) : base(player, next) { }
        public override int StolenBases => 1;

        public override string ToString() => $"{Player.DisplayName} stole {AdvanceTo.GetDescription().ToLower()}.  ";
    }
    public class StealOfHome : RunScored
    {
        public StealOfHome()
        {
            _eventScoring = "SB";
        }
        public StealOfHome(BaseRunner player, OnBase next)
        {
            AdvanceRunner(player, next);
            AdvanceTo = OnBase.None;
        }
        public override int StolenBases => 1;
        public override string ToString() => $"{Player.LastName} stole home.  ";
    }
    public class StealOfHomeUnearned : StealOfHome
    {
        public override bool RunIsEarned => false;
    }
    public class OutStealing : OutOnBases
    {
        public OutStealing()
        {
            _eventScoring = "CS";
        }
        public OutStealing(BaseRunner runner) : base(runner, OnBase.None) { }
        public override int CaughtStealing => 1;
        public override string ToString() => $"{Player?.DisplayName} caught stealing.  ";
    }
    public class OutOnBases : RunningEvent
    {
        public OutOnBases() { }
        public OutOnBases(BaseRunner runner, OnBase outAt) : base(runner, OnBase.None)
        {
            OutAt = outAt;
            _scoringIsRequired = true;
        }
        public override int Outs => 1;
        internal override XElement Xml
        {
            get
            {
                var xml = base.Xml;
                xml.Add(new XElement("OutAt", OutAt.ToString()));
                return xml;
            }
        }
        public override string ToString() => $"{Player.DisplayName} out at {(OutAt == OnBase.None ? "Home" : OutAt.ToString().ToLower())}.  ";
    }
    public class PickOff : OutOnBases
    {
        public PickOff() { }
        public PickOff(BaseRunner runner, OnBase outAt) : base(runner, outAt)
        {
            OutAt = outAt;
            _scoringIsRequired = true;
        }
        public override string ToString() => $"{Player.DisplayName} picked off of {OutAt.ToString().ToLower()}.  ";
    }
    public abstract class RunnerAdvanceOnBatteryError : RunnerAdvance
    {
        public RunnerAdvanceOnBatteryError() { }
        protected RunnerAdvanceOnBatteryError(IList<RunningEvent> advances)
        {
            foreach (var advance in advances)
                AddEvent(advance);
        }
        internal override string EventText
        {
            get
            {
                var sb = new StringBuilder();
                //$"{(Player != null ? $"{Player.LastName}" : "Player")}");
                if (Events.OfType<RunningEvent>().Any())
                {
                    foreach (var rev in Events.OfType<RunningEvent>())
                        sb.Append(rev.EventText.Trim().Replace(".", ", "));
                    sb.Remove(sb.Length - 2, 2);
                }
                sb.Append($" on {EventDescr}");
                if (ResponsiblePitcher is null)
                    sb.Append(".  ");
                else
                    sb.Append($" by {ResponsiblePitcher.LastName}.  ");
                return sb.ToString();
            }
        }
        protected abstract string EventDescr { get; }
        //internal override string EventString(Player runner)
        //{
        //    return $"{runner.LastName} advanced on {EventDescr} {(Player is null ? "" : $"by {Player.LastName}.  ")}";
        //}
        //public override string ToString() => EventText;
    }
    public class PassedBall : RunnerAdvanceOnBatteryError
    {
        public PassedBall() => _eventScoring = "PB";
        protected override string EventDescr => "passed ball";
    }
    public class WildPitch : RunnerAdvanceOnBatteryError
    {
        public WildPitch() => _eventScoring = "WP";
        protected override string EventDescr => "wild pitch";
    }
    public class Balk : RunnerAdvanceOnBatteryError
    {
        public Balk() => _eventScoring = "BK";
        protected override string EventDescr => "balk";
    }
    public class RunningEventComparer : IEqualityComparer<InningEvent>
    {
        public bool Equals(InningEvent x, InningEvent y)
        {
            if (!(x is RunningEvent rex) || !(y is RunningEvent rey))
                return false;
            if (rex.ResponsiblePitcher is null || rey.ResponsiblePitcher is null)
                return false;
            if (rex.Player is null || rey.Player is null)
                return false;
            return rex.Player.Equals(rey.Player) && rex.ResponsiblePitcher.Equals(rey.ResponsiblePitcher) && x.Sequence == y.Sequence && x.GetType().Equals(y.GetType());
        }

        public int GetHashCode(InningEvent obj)
        {
            if (!(obj is RunningEvent rev))
                return obj.GetHashCode();
            return (rev.Player?.GetHashCode() ?? 0 * rev.Sequence * rev.GetType().GetHashCode()).GetHashCode();
        }
    }
}
