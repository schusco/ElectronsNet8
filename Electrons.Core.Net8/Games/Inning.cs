using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Games
{
    public class Inning
    {
        private Inning(bool noAdvance)
        {
            _events = new Stack<AtBat>();
            _navEvents = new Stack<AtBat>();
            _inningStartsWithLastBatterFromPreviousInning = noAdvance;
        }
        internal event EventHandler InningEnded;
        internal event EventHandler<InningEventArgs> ReliefPitcherEntered;
        public event EventHandler InningUpdated;
        public event EventHandler RunnerAdvance;
        internal event EventHandler RunScored;
        public int Number { get; private set; }
        public Team Team { get; private set; }
        public HalfInning Half { get; private set; }
        public IOrderedEnumerable<AtBat> Events => _events.OrderBy(o => o.Sequence);
        internal AtBat NextBatter()
        {
            var sameHitter = _inningStartsWithLastBatterFromPreviousInning && !_events.Any();
            var nextBatter = Team.NextHitter(sameHitter);
            var ab = _navEvents.Any() ? _navEvents.Pop() : NewAb(nextBatter);
            _events.Push(ab);
            OnInningUpdated();
            return ab;
        }
        private AtBat NewAb(Player batter)
        {
            var lastBatter = Events.LastOrDefault();
            var seq = (lastBatter?.Sequence ?? 0) + 1;
            var ab = new AtBat(this, seq, batter, CurrentPitcher);
            batter.AdAtBat(ab);
            AddAbEvents(ab, this);
            return ab;
        }
        private void Ab_RunningEventAdded(object sender, InningEventArgs e)
        {
            RunnerAdvance?.Invoke(sender, e);
            if (Outs == 3)
                InningOver();
        }

        internal void Ab_RunScored(object sender, InningEventArgs e)
        {
            RunScored?.Invoke(sender, e);
        }
        internal void Ab_ReliefPitcherEntered(object sender, InningEventArgs e)
        {
            ReliefPitcherEntered?.Invoke(sender, e);
        }
        private void Ab_PinchHitter(object sender, InningEventArgs e)
        {
            var sub = e.Event as Substitution;
            var ab = NewAb(sub.NewPlayer);
            _events.Push(ab);
            InningUpdated?.Invoke(this, e);
        }
        public void AddCourtesyRunner(Player runner, BaseRunner previousRunner)
        {
            CurrentAb.AddEvent(Substitution.CourtesyRunner(runner, previousRunner.Runner));
        }
        internal void InningOver()
        {
            InningIsFinished = true;
            InningEnded?.Invoke(this, new EventArgs());
        }
        internal void AddAb(AtBat ab) => _events.Push(ab);
        internal void AtBat_AtBatFinished(object sender, EventArgs e)
        {
            OnInningUpdated();
            if (Outs > 3)
                throw new BaseballGameException("Too many outs");
            if (Outs == 3)
                InningOver();
        }
        [JsonIgnore]
        public RunnersOn CurrentRunners
        {
            get
            {
                var runners = new RunnersOn();
                foreach (var ab in Events)
                {
                    foreach (var ev in ab.Events)
                    {
                        if (ev is RunnerAdvanceOnBatteryError be)
                            AdvanceRelatedEvents(be, runners);
                        else if (ev is AdvancedOnError eAdv)
                        {
                            runners.AdvanceRunners(eAdv, eAdv.ReachedOnError);
                            AdvanceRelatedEvents(eAdv, runners);
                        }
                        else if (ev is RunScoredOnError soe)
                        {
                            runners.AdvanceRunners(soe, soe.ReachedOnError);
                            AdvanceRelatedEvents(soe, runners);
                        }
                        else if (ev is RunningEvent rev)
                            runners.AdvanceRunners(rev, rev.ReachedOnError);
                        else if (ev is PinchRunner sub)
                            runners.PinchRun(sub.Replaced, sub.NewPlayer);
                        else if (ev is CourtesyRunner crunner)
                            runners.PinchRun(crunner.Replaced, crunner.NewPlayer);
                        else if (ev is AtBatResult)
                            AdvanceRelatedEvents(ev, runners);
                    }
                }
                if (!(CurrentAb.Result is null) && !CurrentAb.Events.Any(a => a is AtBatResult))
                {
                    foreach (var rev in CurrentAb.Result.RunningEvents.OrderBy(o => o.Sequence))
                    {
                        runners.AdvanceRunners(rev, rev.ReachedOnError);
                        AdvanceRelatedEvents(rev, runners);
                    }
                }
                return runners;
            }
        }
        [JsonIgnore]
        public List<RunningEvent> AdvancingRunners
        {
            get
            {
                List<RunningEvent> revs = Events.SelectMany(s => s.AdvancingRunners).ToList();
                return revs.Distinct(new RunningEventComparer()).OfType<RunningEvent>().ToList();
            }
        }
        private void AdvanceRelatedEvents(InningEvent rev, RunnersOn runners)
        {
            foreach (var resultEv in rev.Events.OfType<RunningEvent>())
            {
                runners.AdvanceRunners(resultEv, resultEv.ReachedOnError);
                if (resultEv.Events.OfType<RunningEvent>().Any())
                    AdvanceRelatedEvents(resultEv, runners);
            }
        }
        [JsonIgnore]
        public AtBat CurrentAb => Events.LastOrDefault() ?? NextBatter();
        [JsonIgnore]
        public IEnumerable<KeyValuePair<OnBase, BaseRunner>> Runners
        {
            get
            {
                if (CurrentRunners.RunnerOnFirst)
                    yield return new KeyValuePair<OnBase, BaseRunner>(OnBase.First, CurrentRunners.OnFirst);
                if (CurrentRunners.RunnerOnSecond)
                    yield return new KeyValuePair<OnBase, BaseRunner>(OnBase.Second, CurrentRunners.OnSecond);
                if (CurrentRunners.RunnerOnThird)
                    yield return new KeyValuePair<OnBase, BaseRunner>(OnBase.Third, CurrentRunners.OnThird);
            }
        }
        public Pitcher CurrentPitcher { get; private set; }
        public void SetCurrentPitcher(Pitcher pitcher)
        {
            CurrentPitcher = pitcher;
            foreach (var ab in _events.Where(w => w.Pitcher is null || w.Pitcher.IsUnknown))
                ab.SetPitcher((Player)pitcher);
            OnInningUpdated();
        }
        public bool InningIsFinished { get; private set; }
        [JsonIgnore]
        public int Outs => _events.Sum(s => s.Outs);
        [JsonIgnore]
        public int Runs => _events.Sum(s => s.Runs);
        [JsonIgnore]
        public int Hits => _events.Sum(s => s.Hits);
        [JsonIgnore]
        public int Errors => _events.Sum(s => s.Errors);
        [JsonIgnore]
        public bool InningStarted => Events.Any();
        internal RunningEvent MoveBatter(OnBase toBase, bool reachedOnError)
        {
            var adv = new RunnerAdvanceFromAb(BaseRunner.Create(CurrentAb.Batter, (Player)CurrentPitcher, error: reachedOnError), toBase);
            //CurrentAb.AddEvent(adv);
            return adv;
        }
        internal IEnumerable<RunningEvent> ForceRunners(bool reachedOnError = false, bool chargeRunAsEarned = true)
        {
            var runners = CurrentRunners;
            switch ((int)runners.Runners)
            {
                case 0:
                case 2:
                case 4:
                case 6:
                    yield return MoveBatter(OnBase.First, reachedOnError);
                    break;
                case 1:
                case 5:
                    yield return AdvanceRunner(runners.OnFirst, OnBase.Second, AdvanceReason.Ab);
                    yield return MoveBatter(OnBase.First, reachedOnError);
                    break;
                case 3:
                    yield return AdvanceRunner(runners.OnSecond, OnBase.Third, AdvanceReason.Ab);
                    yield return AdvanceRunner(runners.OnFirst, OnBase.Second, AdvanceReason.Ab);
                    yield return MoveBatter(OnBase.First, reachedOnError);
                    break;
                case 7:
                    yield return ScoreRunner(runners.OnThird, chargeAsEarned: chargeRunAsEarned);
                    yield return AdvanceRunner(runners.OnSecond, OnBase.Third, AdvanceReason.Ab);
                    yield return AdvanceRunner(runners.OnFirst, OnBase.Second, AdvanceReason.Ab);
                    yield return MoveBatter(OnBase.First, reachedOnError);
                    break;
            }
        }

        internal RunningEvent AdvanceRunner(BaseRunner runner, OnBase toBase, AdvanceReason reason)//=AdvanceReason.Ab)
        {
            RunningEvent ev;
            switch (reason)
            {
                case AdvanceReason.Error:
                    ev = new AdvancedOnError(runner, toBase);
                    break;
                case AdvanceReason.Ab:
                    ev = new RunnerAdvanceFromAb(runner, toBase);
                    break;
                case AdvanceReason.Throw:
                    ev = new RunnerAdvanceOnThrow(runner, toBase);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }
            return ev;
        }
        internal RunningEvent ScoreRunner(BaseRunner runner, bool scoredOnError = false, bool chargeAsEarned = true)
        {
            RunningEvent ev;
            if (scoredOnError)
                ev = new RunScoredOnError(runner);
            else if (runner.ReachedOnError || !chargeAsEarned)
                ev = new UnearnedRunScored(runner);
            else
                ev = new RunScored(runner);
            //CurrentAb.AddEvent(ev);
            return ev;
        }
        internal IEnumerable<RunningEvent> AdvanceAllRunners(int bases, bool includeBatter = true, bool reachedOnError = false, bool chargeRunAsEarned = true)
        {
            if (bases >= 0)
            {
                var runners = CurrentRunners;
                if (runners.RunnerOnThird)
                    yield return ScoreRunner(runners.OnThird, chargeAsEarned: chargeRunAsEarned);
                if (runners.RunnerOnSecond)
                {
                    if (bases > 1)
                        yield return ScoreRunner(runners.OnSecond, chargeAsEarned: chargeRunAsEarned);
                    else
                        yield return AdvanceRunner(runners.OnSecond, OnBase.Third, AdvanceReason.Ab);
                }
                if (runners.RunnerOnFirst)
                {
                    if (bases > 2)
                        yield return ScoreRunner(runners.OnFirst, chargeAsEarned: chargeRunAsEarned);
                    else if (bases > 1)
                        yield return AdvanceRunner(runners.OnFirst, OnBase.Third, AdvanceReason.Ab);
                    else
                        yield return AdvanceRunner(runners.OnFirst, OnBase.Second, AdvanceReason.Ab);
                }
                if (includeBatter)
                {
                    if (bases > 3)
                        yield return ScoreRunner(BaseRunner.Create(CurrentAb.Batter, (Player)CurrentPitcher, error: reachedOnError), chargeAsEarned: chargeRunAsEarned);
                    else if (bases == 3)
                        yield return AdvanceRunner(BaseRunner.Create(CurrentAb.Batter, (Player)CurrentPitcher, error: reachedOnError), OnBase.Third, AdvanceReason.Ab);
                    else
                        yield return AdvanceRunner(BaseRunner.Create(CurrentAb.Batter, (Player)CurrentPitcher, error: reachedOnError), (OnBase)bases, AdvanceReason.Ab);
                }
            }
        }
        internal void EndedOnWalkOff()
        {
            InningIsFinished = true;
            InningEnded?.Invoke(this, new EventArgs());
        }
        internal void PreviousAtBat()
        {
            var inning = _events.Pop();
            _navEvents.Push(inning);
            Team.SetNextHitter(CurrentAb.Batter);
            OnInningUpdated();
        }
        internal void NextAtBat()
        {
            if (_navEvents.Any())
            {
                var inning = _navEvents.Pop();
                _events.Push(inning);
                Team.SetNextHitter(CurrentAb.Batter);
                OnInningUpdated();
            }
        }
        internal void MoveCurrent()
        {
            var cnt = _navEvents.Count;
            for (int i = 0; i < cnt; i++)
                NextAtBat();
        }
        internal static Inning Load(XElement inningEl, BaseballGame game, Team team, Team opposition)
        {
            var inning = new Inning(false)
            {
                Half = (HalfInning)Enum.Parse(typeof(HalfInning), inningEl.Attribute("half").Value),
                Number = int.Parse(inningEl.Attribute("number").Value),
                Team = team,
                CurrentPitcher = opposition.CurrentPitcher
            };
            inning.ReliefPitcherEntered += game.Inning_ReliefPitcherEntered;
            game.PushInning(inning);
            inning.RunScored += game.Inning_RunScored;
            foreach (var el in inningEl.Descendants("AtBat"))
            {
                var ab = AtBat.Load(el, inning, opposition);
                AddAbEvents(ab, inning);
                //inning._events.Push(ab);
            }
            inning.InningIsFinished = inning.Outs == 3;
            return inning;
        }
        [JsonIgnore]
        internal XElement Xml
        {
            get
            {
                var xel = new XElement("Inning");
                xel.SetAttributeValue("number", Number);
                xel.SetAttributeValue("half", Half);
                foreach (var ev in Events)
                    xel.Add(ev.Xml);
                return xel;
            }
        }
        private static void AddAbEvents(AtBat ab, Inning inning)
        {
            ab.AtBatFinished += inning.AtBat_AtBatFinished;
            ab.RunningEventAdded += inning.Ab_RunningEventAdded;
            ab.RunScored += inning.Ab_RunScored;
            ab.PinchHitterEntered += inning.Ab_PinchHitter;
        }
        internal bool IsOnBase(Player player) => CurrentRunners.RunnersOnBase.Any(a => a.Runner == player);
        internal static Inning Create(int number, HalfInning half, Team battingTeam, Pitcher pitcher, bool noAdvance = false) =>
            new Inning(noAdvance) { Number = number, Half = half, Team = battingTeam, CurrentPitcher = pitcher };
        internal static Inning TopOfOne(Team team, Pitcher pitcher) => new Inning(false) { Number = 1, Half = HalfInning.Top, Team = team, CurrentPitcher = pitcher };
        public override string ToString() => $"{Half} of {Number}";
        [JsonIgnore]
        public string InningSummary => $"{Runs} {(Runs == 1 ? "Run" : "Runs")}, {Hits} {(Hits == 1 ? "Hit" : "Hits")}, {Errors} {(Errors == 1 ? "Error" : "Errors")}, {GetLeftOn()} Left";
        public int GetLeftOn() => !(InningIsFinished || Outs == 3) ? 0 : Runners.Count();
        [JsonIgnore]
        public IList<Substitution> Substitutions => Events.SelectMany(s => s.Events).OfType<Substitution>().ToList();
        internal void RemoveLastAb() => _events.Pop();
        [JsonIgnore]
        public int SpotInLineup
        {
            get
            {
                var spot = Team.Lineup.IndexOf(CurrentAb.Batter);
                return spot + 1;
            }
        }

        private void OnInningUpdated() => InningUpdated?.Invoke(this, EventArgs.Empty);
        internal void SetPitcher(Pitcher currentPitcher) => CurrentPitcher = currentPitcher;
        private readonly bool _inningStartsWithLastBatterFromPreviousInning;
        private readonly Stack<AtBat> _events;
        private readonly Stack<AtBat> _navEvents;


    }
}
