using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Games
{
    public abstract class InningEvent
    {
        protected InningEvent()
        {
            _events = new Stack<InningEvent>();
            _fielders = new List<string>();
            _eventScoring = string.Empty;
        }
        internal event EventHandler FieldersUpdated;
        internal event EventHandler<InningEventArgs> RunningEventAdded;
        public int Sequence { get; internal set; }
        internal abstract string EventText { get; }
        [JsonIgnore]
        public virtual int Outs => 0;
        [JsonIgnore]
        public virtual int Hits => 0;
        [JsonIgnore]
        public virtual int Errors => _events.Sum(s => s.Errors);
        [JsonIgnore]
        public virtual int Runs
        {
            get
            {
                var runs = 0;
                foreach (var ev in _events.ToList())
                {
                    runs += ev.Runs;
                    //foreach (var childEv in ev.Events)
                    //{
                    //    runs += childEv.Runs;
                    //}
                }
                return runs;
            }
        }
        public IEnumerable<InningEvent> Events => _events.OrderBy(o => o.Sequence);
        [JsonIgnore]
        public IEnumerable<RunningEvent> RunningEvents
        {
            get
            {
                if (this is RunningEvent rev)
                    yield return rev;
                foreach (var ev in _events.OfType<RunningEvent>())
                    yield return ev;
                //foreach (var ev in _events.OfType<RunScored>())
                //    yield return ev;
            }
        }
        [JsonIgnore]
        public virtual string Scoring
        {
            get
            {
                var scoringList = new List<string>();
                if (!string.IsNullOrEmpty(_eventScoring))
                    scoringList.Add(_eventScoring);
                foreach (var fielder in _fielders)
                    scoringList.Add(fielder);
                foreach (var ev in _events.Where(w => !string.IsNullOrEmpty(w.Scoring)))
                    scoringList.Add(ev.Scoring);

                return string.Join("-", scoringList);
            }
        }
        public void AddFielder(Position p) => AddFielder(p.PositionNumber.ToString());
        internal void AddFielder(string fielder)
        {
            if (_events.Any(a => a.IsScoringRequired()))
            {
                var ev = _events.Last(l => l.IsScoringRequired());
                ev._fielders.Add(fielder);
            }
            else
                _fielders.Add(fielder);
            FieldersUpdated?.Invoke(this, new EventArgs());
        }
        public void AddEvent(InningEvent ev)
        {
            var seq = _events.Any() ? _events.Max(s => s.Sequence) : 0;
            seq++;
            ev.Sequence = seq;
            _events.Push(ev);
            RunningEventAdded?.Invoke(this, new InningEventArgs(ev));
        }
        internal string Fielders => string.Join("-", _fielders.Union(Events.SelectMany(s => s._fielders)));
        [JsonIgnore]
        internal string FieldingPlayer => _fielders.Union(Events.SelectMany(s => s._fielders)).FirstOrDefault();
        internal void RemoveLastFielder()
        {
            _fielders.RemoveAt(_fielders.IndexOf(_fielders.Last()));
            FieldersUpdated?.Invoke(this, new EventArgs());
        }
        [JsonIgnore]
        internal virtual XElement Xml
        {
            get
            {
                return
                    new XElement(GetType().Name,
                        new XElement("Sequence", Sequence),
                        new XElement("Fielders", _fielders.ToList().Select(s => new XElement("Position", AllPositions[s].PositionString))),
                        new XElement("Events", Events.Select(s => new XElement("InningEvent", s.Xml)))
                        );
            }
        }
        internal static InningEvent Load(XElement el, string batter)
        {
            InningEvent evInstance = GetInstance(el.Name.LocalName);
            evInstance.Sequence = int.Parse(el.Descendants().First(s => s.Name == "Sequence").Value);
            foreach (var ev in el.Descendants().First(s => s.Name == "Fielders").Descendants().Where(w => w.Name == "Position"))
                evInstance.AddFielder(Position.FromString(ev.Value).PositionNumber.ToString());
            var targetType = evInstance.GetType();
            if (targetType.IsSubclassOf(typeof(RunningEvent)))
                evInstance = RunningEvent.Load((RunningEvent)evInstance, el);
            if (evInstance is Substitution || targetType.IsSubclassOf(typeof(Substitution)))
                evInstance = Substitution.Load((Substitution)evInstance, el);
            if (evInstance is AtBatResult || targetType.IsSubclassOf(typeof(AtBatResult)))
                evInstance = AtBatResult.Load((AtBatResult)evInstance, el);
            var eventEl = el.Descendants().FirstOrDefault(s => s.Name == "Events");
            if (!(eventEl is null))
            {
                foreach (var ev in eventEl.Descendants().Where(w => w.Name == "InningEvent" && w.Parent == eventEl))
                    evInstance.AddEvent(Load(ev.Descendants().First(), batter));
            }
            if (evInstance is AtBatResult ab)
                ab.PlayerName = batter;
            return evInstance;
        }
        internal static InningEvent GetInstance(string type)
        {
            var typs = Assembly.GetAssembly(typeof(InningEvent)).GetTypes();
            var targetType = typs.Single(s => s.Name == type);
            return (InningEvent)Activator.CreateInstance(targetType);
        }
        public bool HasFielders => _fielders.Any();
        protected Dictionary<string, Position> AllPositions => new Dictionary<string, Position>
        {
            {"1",Position.P },
            {"2",Position.C },
            {"3",Position._1B },
            {"4",Position._2B },
            {"5",Position._3B },
            {"6",Position.SS },
            {"7",Position.LF },
            {"8",Position.CF },
            {"9",Position.RF }
        };
        public virtual bool IsScoringRequired()
        {
            if (_scoringIsRequired)
                return true;
            if (!_events.Any())
                return false;
            foreach (var ev in _events)
            {
                if (ev._scoringIsRequired)
                    return true;
            }
            return false;
        }
        internal InningEvent AddFielders(AtBatResult result)
        {
            foreach (var fielder in result._fielders)
                AddFielder(fielder);
            return this;
        }
        internal virtual void ClearFielding()
        {
            _fielders.Clear();
            FieldersUpdated?.Invoke(this, new EventArgs());
        }
        public virtual string EventString(Player batter)
        {
            var sb = new StringBuilder($"{batter.DisplayName} {_result}");
            if (_fielders.Any())
                sb.Append(FielderString);
            sb.Append(".  ");
            AddRunningEventsToString(sb, batter);
            return sb.ToString();
        }
        protected string FielderString => $" to {AllPositions[FieldingPlayer].PositionText}";
        protected internal void AddRunningEventsToString(StringBuilder sb, Player batter)
        {
            bool scoringAdded = false;
            bool advancesAdded = false;
            foreach (var rev in RunningEvents.OrderBy(o => o.Sequence))
            {
                if (rev is OutOnBases)
                    sb.Append(rev);
                else if (rev is RunScored)
                {
                    if (!scoringAdded)
                    {
                        AddScoringPlays(sb, Events.OfType<RunScored>().Where(w => w.Player != batter));
                        scoringAdded = true;
                    }
                }
                else if (rev is RunnerAdvanceFromAb)
                {
                    if (rev.Player == batter && (rev.GetType() == typeof(RunnerAdvance) || rev.GetType().IsSubclassOf(typeof(RunnerAdvance))))
                        continue;
                    if (RunningEvents.Any(a => a.Sequence > rev.Sequence && rev.Player == a.Player))
                        continue;
                    if (RunningEvents.Any(a => a.Sequence > rev.Sequence && a.RunningEvents.Any(b => b.Player == rev.Player)))
                        continue;
                    sb.Append(rev);
                }
                else if (rev is RunnerAdvanceOnThrow)
                {
                    sb.Append(rev);
                }
                else if (rev is AdvancedOnError)
                    sb.Append(rev);
            }
        }
        public IEnumerable<RunningEvent> AdvancingRunners
        {
            get
            {
                foreach (var ev in _events)
                {
                    if (ev is RunningEvent rev)
                        yield return rev;
                    foreach (var childEvent in ev.AdvancingRunners)
                        yield return childEvent;
                }
            }
        }
        private static void AddScoringPlays(StringBuilder sb, IEnumerable<RunScored> scoringPlays)
        {
            if (scoringPlays.Count() == 1)
                sb.Append(scoringPlays.Single());
            else
            {
                var playsToInclude = scoringPlays.Where(w => !(w is RunScoredOnError)).ToList();
                var scoredOnError = scoringPlays.Where(w => w is RunScoredOnError);
                foreach (var score in playsToInclude)
                {
                    if (score == playsToInclude.First())
                        sb.Append(score.Player.DisplayName);
                    else if (score == playsToInclude.Last())
                        sb.Append($" and {score.Player.DisplayName}");
                    else
                        sb.Append($", {score.Player.DisplayName}");

                }

                if (playsToInclude.Any())
                    sb.Append(" scored.  ");

                foreach (var score in scoredOnError)
                    sb.Append(score);
            }
        }

        private void AddRunnerAdvanceEvents(Player batter, IEnumerable<RunningEvent> runningEvents, StringBuilder sb)
        {
            foreach (var rScEv in runningEvents)
            {


            }
        }
        protected readonly Stack<InningEvent> _events;
        protected readonly List<string> _fielders;
        protected string _eventScoring;
        protected string _result;
        protected bool _scoringIsRequired;

        internal virtual void ClearScoringRequired()
        {
            _scoringIsRequired = false;
        }

        internal virtual bool RemoveRunningEvent(RunningEvent rev)
        {
            var evs = _events.ToList();
            if (evs.Remove(rev))
            {
                _events.Clear();
                foreach (var ev in evs)
                    _events.Push(ev);
                return true;
            }
            return false;
        }
    }
    public abstract class AtBatResult : InningEvent
    {
        public virtual int Ab => 1;
        public virtual int Hbp => 0;
        public virtual int Walks => 0;
        public virtual int StrikeOuts => 0;
        public virtual int Doubles => 0;
        public virtual int Triples => 0;
        public virtual int HomeRuns => 0;
        public virtual int Sac => 0;
        public virtual int SacFly => 0;
        public virtual bool FinishedAb => true;
        public string PlayerName { get; protected internal set; }
        public string Result => _result;
        public override string Scoring => base.Scoring;
        internal override string EventText => _result;

        internal void Copy(AtBatResult result)
        {
            foreach (var resev in result.Events)
                AddEvent(resev);
            foreach (var fielder in result._fielders)
                AddFielder(fielder);
            PlayerName = result.PlayerName;
        }
        internal void SetFieldLocation(FieldLocation loc)
        {
            _fieldLocation = loc;
        }
        internal AtBatResult Update(UnfinshedAb result)
        {
            AtBatResult ev;
            var outsOnBases = result.Events.OfType<OutOnBases>().Where(w => !w.HasFielders).Count();
            result.Events.OfType<OutOnBases>().Where(w => w.IsScoringRequired()).ToList().ForEach(f => f.ClearScoringRequired());
            if (outsOnBases >= 2)
                ev = new TriplePlay();
            else if (outsOnBases == 1)
                ev = new DoublePlay();
            else
                ev = new GroundOut();
            ev.Copy(result);
            return ev;
        }
        internal AtBatResult UpdateFlyout(FlyOut fo)
        {
            AtBatResult ev;
            var outsOnBases = fo.Events.OfType<OutOnBases>().Count();
            fo.Events.OfType<OutOnBases>().Where(w => w.IsScoringRequired()).ToList().ForEach(f => f.ClearScoringRequired());
            if (outsOnBases >= 2)
                ev = new LinedIntoTriplePlay();
            else if (outsOnBases == 1)
                ev = new LinedIntoDoublePlay();
            else
                return fo;
            ev.Copy(fo);
            return ev;
        }
        internal AtBatResult Update(AB ab, IList<RunningEvent> advances = null)
        {
            if (FinishedAb)
                return this;
            var ev = GetInstance(ab.ToString()) as AtBatResult;

            ev.Copy(this);
            if (!(advances is null))
            {
                foreach (var rev in advances)
                    ev.AddEvent(rev);
            }
            return ev;
        }
        public void BackOne(AtBat ab)
        {
            if (Events.OfType<RunningEvent>().Any())
                _events.OfType<RunningEvent>().Last().BackOne(ab);
        }

        //protected void AddRunningEvents(StringBuilder sb, Player batter)
        //{
        //    if (_events.OfType<RunScored>().Any())
        //    {
        //        var revs = _events.OfType<RunScored>();
        //        sb.Append(string.Join(", ", revs.Where(w => w.Player != batter).Select(s => s.Player.LastName)));
        //        sb.Append(" scored");
        //        if (revs.Any(a => a is RunScoredOnError))
        //        {
        //            var errorEv = (RunScoredOnError)revs.First(f => f is RunScoredOnError rs);
        //            sb.Append(errorEv.ErrorString);
        //            if (revs.Any(a=> a.Player == batter))
        //            {
        //                var batterEv = revs.First(a => a.Player == batter);
        //                sb.Append(batterEv.ToString());
        //            }
        //        }
        //        else
        //            sb.Append(".  ");
        //    }
        //    foreach (var ev in RunningEvents.Where(w => !(w is RunScored)))
        //    {
        //        if (ev.Player == batter)
        //            continue;
        //        if (ev is AdvancedOnError)
        //            sb.Append(ev.EventString(batter));
        //        if (Events.Any(a=>a.))
        //    }
        //}
        internal override XElement Xml
        {
            get
            {
                var xml = base.Xml;
                xml.Add(new XElement("FieldLocation", _fieldLocation));
                return xml;
            }
        }
        internal static InningEvent Load(AtBatResult ab, XElement el)
        {
            var floc = el.Descendants().SingleOrDefault(s => s.Name == "FieldLocation");
            if (floc != null)
                ab.SetFieldLocation((FieldLocation)Enum.Parse(typeof(FieldLocation), floc.Value));
            return ab;
        }
        protected FieldLocation _fieldLocation;
        public override string ToString() => _result;

        internal override void ClearScoringRequired()
        {
            _scoringIsRequired = false;
            foreach (var ev in _events.Where(w => w.IsScoringRequired()))
                ev.ClearScoringRequired();
        }

        internal void AddRunningEvent(BaseRunner runner, OnBase tobase, AdvanceReason reason)
        {
            _events.Push(RunnerAdvance.Create(runner, tobase, reason));
        }
    }
    internal class UnfinshedAb : AtBatResult
    {
        public override int Ab => 0;
        private UnfinshedAb() { }
        public override bool FinishedAb => false;
        internal string InningText { get; set; }
        internal override string EventText { get => $"{PlayerName} batting in the {InningText}.  "; }
        internal static UnfinshedAb Create(Player batter, Inning inning)
        {
            return new UnfinshedAb
            {
                InningText = $"{inning.Half} of {inning.Number}",
                PlayerName = batter.LastName
            };
        }
    }
    public class ErrorOnFoul : InningEvent
    {
        public ErrorOnFoul()
        {
            _result = "Error on foul ball";
            _scoringIsRequired = true;
            _eventScoring = "E";
        }
        internal override string EventText => _result;
        public override string EventString(Player batter) => $"{EventText} {FielderString}.  ";
        public override int Errors => 1;
        public override string ToString() => EventText;

    }
}
