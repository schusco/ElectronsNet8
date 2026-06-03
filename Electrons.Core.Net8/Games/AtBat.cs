using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Games
{
    public class AtBat
    {
        public AtBat()
        {
            _events = new List<InningEvent>();
        }
        public AtBat(Inning inning, int sequence, Player batter, Pitcher pitcher) : this()
        {
            _inning = inning;
            Sequence = sequence;
            Batter = batter;
            Pitcher = pitcher ?? (Pitcher)Player.Unknown();
            Result = UnfinshedAb.Create(batter, inning);
        }

        public event EventHandler<InningEventArgs> AtBatFinished;
        public event EventHandler<InningEventArgs> AtBatEventAdded;
        public event EventHandler FieldersUpdated;
        public event EventHandler<InningEventArgs> PinchHitterEntered;
        public event EventHandler<InningEventArgs> ReliefPitcherEntered;
        public event EventHandler<InningEventArgs> RunningEventAdded;
        public event EventHandler<InningEventArgs> RunScored;
        public event EventHandler ScoringUpdated;
        [JsonIgnore]
        public static Func<AtBat, bool> WithTwoStrikesFilter => ab => ab.Strikes == 2;
        [JsonIgnore]
        public static Func<AtBat, bool> WithRispFilter => ab => ab.RunnersOnForAb.HasFlag(OnBase.Second) || ab.RunnersOnForAb.HasFlag(OnBase.Third);
        public Player Batter { get; private set; }
        public Pitcher Pitcher { get; private set; }
        public int Sequence { get; private set; }
        public IOrderedEnumerable<InningEvent> Events { get => _events.OrderBy(o => o.Sequence); private set => _events = value.ToList(); }
        [JsonIgnore]
        public int Outs => _events.Where(w => !w.IsScoringRequired()).Sum(m => m.Outs);
        [JsonIgnore]
        public int Runs => _events.Sum(m => m.Runs);
        [JsonIgnore]
        internal int Hits => _events.Sum(m => m.Hits);
        [JsonIgnore]
        internal int Errors => _events.Sum(s => s.Errors);
        public int RunsBattedIn { get; private set; }
        public void UndoRunScored()
        {
            if (Result.RunningEvents.Any(a => a is RunScored))
            {
                foreach (var inningEvent in Result.RunningEvents)
                {
                    if (inningEvent is RunScored)
                    {
                        if (Result.RemoveRunningEvent(inningEvent) && Result.RunningEvents.All(a => a.Player != inningEvent.Player))
                            Result.AddRunningEvent(BaseRunner.Create(inningEvent.Player, inningEvent.ResponsiblePitcher, inningEvent.OriginalRunner), OnBase.Third, AdvanceReason.Ab);
                    }
                }
            }
            else if (Events.LastOrDefault()?.Events.Any(a => a is RunScored) ?? false)
            {
                foreach (var ev in Events.Last().Events)
                    if (ev is RunScored rev)
                        Events.Last().RemoveRunningEvent(rev);
            }
        }
        internal void AddEvent(InningEvent ev)
        {
            if (ev is UnfinshedAb)
                return;
            var seq = !_events.Any() ? 0 : _events.Max(m => m.Sequence);
            seq++;
            ev.Sequence = seq;
            foreach (var e in ev.Events)
            {
                InningEvent ie = null;
                var comparer = new RunningEventComparer();
                if (Events.Contains(e, comparer))
                {
                    ie = Events.First(s => comparer.Equals(e, s));
                    _events.Remove(ie);
                }

            }
            _events.Add(ev);
            WireEvents(ev);
            OnEventAdded(ev);
        }
        internal void WireEvents(InningEvent ev)
        {
            ev.FieldersUpdated += OnFieldersUpdated;
            //Result.ScoringUpdated += OnScoringUpdated;
        }
        internal void AddEvent(AB ab, BaseRunner runner, OnBase nextBase)
        {
            var ev = InningEvent.GetInstance(ab.ToString());
            if (!!(ev is RunningEvent rev))
            {
                rev.AdvanceRunner(runner, nextBase);
                AddEvent(rev);
            }
            else
                AddEvent(ev);
        }
        internal void AddEvent(AB ab, Player player, IList<RunningEvent> advancingRunners)
        {
            var ev = AddEvent(ab, player);
            foreach (var rev in advancingRunners)
            {
                _events.Remove(rev);
                ev.AddEvent(rev);
            }
            AddEvent(ev);
        }
        internal InningEvent AddEvent(AB ab, Player player)
        {
            var ev = InningEvent.GetInstance(ab.ToString());
            if (!!(ev is RunningEvent rev))
            {
                rev.SetPlayer(player);
                rev.SetPitcher((Player)Pitcher);
                //       AddEvent(rev);
            }
            // else
            //   AddEvent(ev);
            return ev;
        }
        private void OnScoringUpdated(object sender, EventArgs e) => ScoringUpdated?.Invoke(this, e);
        private void OnFieldersUpdated(object sender, EventArgs e) => FieldersUpdated?.Invoke(this, e);
        internal void OnRunningEventChanged(InningEvent ev) => RunningEventAdded?.Invoke(this, new InningEventArgs(ev));
        internal bool Finish(Inning currentInning)
        {
            if (!IsFinished)
            {
                RunnersOnForAb = currentInning.CurrentRunners.Runners;
                AddEvent(_result);
                foreach (var advance in AdvancingRunners.OfType<RunScored>())
                    RunScored?.Invoke(this, new InningEventArgs(advance));                
            }
            AtBatFinished?.Invoke(this, new InningEventArgs(_result));
            if (_result.HasFielders && Events.Any(a => a.IsScoringRequired()))
                _result.ClearScoringRequired();
            return _inning.InningIsFinished;
        }
        [JsonIgnore]
        public OnBase RunnersOnForAb { get; private set; }
        private void OnEventAdded(InningEvent ev)
        {
            if (ev is RunningEvent)
            {
                OnRunningEventChanged(ev);
                foreach (var rev in _events.OfType<RunningEvent>())
                    OnRunningEventChanged(rev);
            }
            else
                AtBatEventAdded?.Invoke(this, new InningEventArgs(ev));
            if (!string.IsNullOrEmpty(ev.Scoring))
                ScoringUpdated?.Invoke(this, new EventArgs());
            if (ev is Substitution subEv && subEv.Replaced == Batter)
                PinchHitterEntered?.Invoke(this, new InningEventArgs(subEv));
            if (ev is ReliefPitcher rp)
                ReliefPitcherEntered?.Invoke(this, new InningEventArgs(rp));
        }
        [JsonIgnore]
        public string CountString => $"{Balls} {(Balls == 1 ? "ball" : "balls")}, {DisplayStrikes} {(DisplayStrikes == 1 ? "strike" : "strikes")}";
        internal bool AddScoring()
        {
            var result = false;
            if (_result is UnfinshedAb && Events.Any(a => a.IsScoringRequired()))
            {
                var ev = Events.First(s => s.IsScoringRequired());
                ev.AddFielders(_result);
                ev.ClearScoringRequired();
                _result.ClearFielding();
                result = true;
            }
            else if (_result is UnfinshedAb && _result.Events.Any(a => a.IsScoringRequired()))
            {
                _result.Events.First(f => f.IsScoringRequired()).AddFielders(_result);
                _result.ClearFielding();
                result = true;
            }
            else if (!(_result is UnfinshedAb) && Events.SelectMany(s => s.Events).Where(w => w.IsScoringRequired()).All(a => a.HasFielders))
            {
                if (_result.IsScoringRequired() && _result.Fielders.Any())
                {
                    _result.ClearScoringRequired();
                    result = true;
                }
            }
            else if (_result is UnfinshedAb && _result.Events.Any(a => a.IsScoringRequired()))
            {
                _events.AddRange(_result.Events.Where(w => w.IsScoringRequired()));
                result = true;
            }
            if (result)
                OnScoringUpdated(this, new EventArgs());
            return result;
        }
        [JsonIgnore]
        public int Balls => Pitches.Where(w => w.Result == PitchResult.Ball).Count();
        [JsonIgnore]
        public IEnumerable<Pitch> Pitches => _events.OfType<Pitch>();
        [JsonIgnore]
        public int Strikes => Pitches.Where(w => w.Result != PitchResult.Ball).Count();
        [JsonIgnore]
        private int DisplayStrikes => Strikes >= 2 ? 2 : Strikes;
        [JsonIgnore]
        public IEnumerable<RunningEvent> AdvancingRunners
        {
            get
            {
                var evs = new List<RunningEvent>();
                foreach (var ev in _events)
                {
                    if (ev is RunningEvent rev)
                        evs.Add(rev);
                    foreach (var childEvent in ev.AdvancingRunners)
                        evs.Add(childEvent);
                }
                foreach (var ev in Result.RunningEvents)
                    evs.Add(ev);
                return evs.Distinct(new RunningEventComparer()).OfType<RunningEvent>();
            }
        }
        [JsonIgnore]
        public IEnumerable<InningEvent> AllRunnerEvents
        {
            get
            {
                var list = new List<InningEvent>();
                foreach (var ev in Events)
                {
                    if ((ev is RunningEvent || ev is PinchRunner) && !ev.Events.Any())
                        list.Add(ev);
                    list.AddRange(ev.RunningEvents.ToList());
                }
                return list;
            }
        }
        [JsonIgnore]
        public IEnumerable<Substitution> Substitutions => _events.OfType<Substitution>();
        internal void UndoScoring(Inning currentInning)
        {
            if (_events.Any())
                _events.Remove(_events.Last());
            Result = UnfinshedAb.Create(Batter, currentInning);
            ScoringUpdated?.Invoke(this, new EventArgs());
        }
        [JsonIgnore]
        internal XElement Xml => new XElement("AtBat", new XElement("Sequence", Sequence),
                                                       new XElement("Batter", Batter.Xml),
                                                       new XElement("Pitcher", Pitcher?.Xml ?? Player.Blank.Xml),
                                                       new XElement("Rbis", RunsBattedIn),
                                                       new XElement("Events", Events.Select(s => new XElement("InningEvent", s.Xml))));
        internal static AtBat Load(XElement el, Inning inning, Team opposition)
        {
            var ab = new AtBat
            {
                Sequence = int.Parse(el.Descendants().First(s => s.Name == "Sequence").Value),
                _inning = inning,
                RunsBattedIn = int.Parse(el.Descendants().FirstOrDefault(s => s.Name == "Rbis")?.Value ?? "0"),
                Batter = GetPlayer(el, "Batter", inning.Team),
                Pitcher = (Pitcher)GetPlayer(el, "Pitcher", opposition)
            };

            ab.RunScored += inning.Ab_RunScored;
            ab.ReliefPitcherEntered += inning.Ab_ReliefPitcherEntered;
            inning.AddAb(ab);

            var eventEl = el.Descendants().First(s => s.Name == "Events")?.Descendants().Where(w => w.Name == "InningEvent" && w.Parent.Parent.Name == "AtBat").ToList();
            if (!(eventEl is null))
            {
                foreach (var inninngEl in eventEl)
                {
                    var runnersForAb = inning.CurrentRunners.Runners;
                    var ev = InningEvent.Load(inninngEl.Descendants().First(), ab.Batter.LastName);
                    if (ev is AtBatResult abr)
                    {
                        abr.ClearScoringRequired();
                        ab.Result = abr;
                    }
                    ab.AddEvent(ev);
                    if (ev is Substitution sub)
                    {
                        if (sub is ReliefPitcher rp)
                        {
                            opposition.AddSubstitutions(new[] { rp });
                            //game.SetCurrentPitcherForTeam(opposition, rp);
                        }
                        else
                        {
                            inning.Team.AddSubstitutions(new[] { sub });
                        }
                    }
                    if (ev is AtBatResult)
                        ab.RunnersOnForAb = runnersForAb;
                }
            }
            if (ab.Result is null)
                ab.Result = UnfinshedAb.Create(ab.Batter, inning);
            foreach (var advance in ab.AdvancingRunners.OfType<RunScored>())
                ab.RunScored?.Invoke(ab, new InningEventArgs(advance));
            return ab;
        }
        private static Player GetPlayer(XElement el, string key, Team team)
        {
            var playerEl = el.Descendants().Single(s => s.Name == key);
            int playerNum = int.Parse(playerEl.Descendants().First(s => s.Name == "Number").Value);
            var playerName = playerEl.Descendants().First(s => s.Name == "LastName").Value;
            var player = team.Roster.FirstOrDefault(s => s.Number == playerNum && s.LastName == playerName);
            return player ?? Player.Unknown(playerNum);
        }
        [JsonIgnore]
        public string Scoring
        {
            get
            {
                var scoring = new StringBuilder(string.Join("-", _events.Where(w => !string.IsNullOrEmpty(w.Scoring)).Select(s => s.Scoring)));
                if (!string.IsNullOrEmpty(Result.Scoring) && !_events.Any(a => a is AtBatResult))
                {
                    if (!string.IsNullOrEmpty(scoring.ToString()))
                        scoring.Append("-");
                    scoring.Append(Result.Scoring);
                }
                return scoring.ToString();
            }
        }
        [JsonIgnore]
        public bool IsFinished => _events.OfType<AtBatResult>().Any() || _events.Any(a => a is PinchHitter);

        internal void UpdateResult(AB ab, IList<RunningEvent> advances = null)
        {
            Result = _result.Update(ab, advances);
            OnScoringUpdated(this, new EventArgs());
            OnRunningEventChanged(_result);
        }
        internal void UpdateResult(UnfinshedAb uab)
        {
            Result = _result.Update(uab);
        }
        internal void UpdateFlyoutResult(FlyOut fo)
        {
            Result = _result.UpdateFlyout(fo);
        }
        internal void RemoveRunningEvent(RunningEvent rev)
        {
            if (_events.Contains(rev))
                _events.Remove(rev);
            else
            {
                var actualEvent = _events.SingleOrDefault(s => s.RunningEvents.Contains(rev));
                actualEvent?.RemoveRunningEvent(rev);
            }
            OnRunningEventChanged(rev);
        }
        internal void SetPitcher(Player newPitcher) => Pitcher = (Pitcher)newPitcher;
        internal void SetBatter(Player newHitter) => Batter = newHitter;
        internal void SetRunsBattedIn(int rbis)
        {
            if (rbis > 4 || rbis < 0)
                throw new BaseballGameException("Invaid rbi number specified.");
            RunsBattedIn = rbis;
        }
        [JsonIgnore]
        public string ResultString => Result.FinishedAb ? Result.EventString(Batter) : ToString();
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!IsFinished)
            {
                sb.Append($"{Batter?.DisplayName ?? ""} batting in the {_inning.Half} of {_inning.Number}.  ");
            }
            foreach (var ev in _events)
            {
                if (ev.GetType() == typeof(PitchInPlay))
                    continue;
                if (ev is RunningEvent rev)
                {

                    if (rev.GetType() == typeof(RunnerAdvanceFromAb) && rev.Player == Batter)
                        continue;
                    if (rev.GetType().IsIn(typeof(StolenBase), typeof(StealOfHome)))
                    {
                        sb.Append(ev.EventString(Batter));
                        continue;
                    }
                    if (_result.Events.OfType<RunningEvent>().Any(a => a.Sequence > rev.Sequence && rev.Player == a.Player))
                        continue;
                    if (rev.GetType().IsIn(typeof(RunnerAdvanceFromAb), typeof(RunScored)))
                        continue;
                }
                sb.Append(ev.EventString(Batter));
            }
            return sb.ToString();

        }
        public AtBatResult Result
        {
            get => _result;
            private set
            {
                _result = value;
                WireEvents(value);
            }
        }

        public string ScoreText => string.Join("  ", Events.Where(s => s.Runs > 0).Select(s => s.EventString(Batter)));

        private List<InningEvent> _events;
        private Inning _inning;
        private AtBatResult _result;
    }
    public class InningEventArgs : EventArgs
    {
        public InningEventArgs(InningEvent ev) => Event = ev;
        public InningEvent Event { get; private set; }
    }
    public class InningChangeEventArgs : EventArgs
    {
        public InningChangeEventArgs(Inning inning) => Inning = inning;
        public Inning Inning { get; private set; }
    }
    public class ScoreChangedEventArgs : EventArgs
    {
        public ScoreChangedEventArgs(AtBat ab) => AtBat = ab;

        public ScoreChangedEventArgs(AtBat ab, List<RunningEvent> advances = null) : this(ab)
        {
            RunnerAdvances = advances;
        }

        public AtBat AtBat { get; private set; }
        public List<RunningEvent> RunnerAdvances { get; }
    }
    public enum AB
    {
        StrikeOut,
        GroundOut,
        Single,
        Double,
        Triple,
        Walk,
        HomeRun,
        FlyOut,
        HitByPitch,
        ReachedOnError,
        ErrorOnFoul,
        FieldersChoice,
        TriplePlay,
        DoublePlay,
        Sacrifice,
        SacrificeFly,
        SacrificeReachOnError,
        PassedBall,
        Balk,//(Player player) => new Balk(player);
        Interference,//(Player player) => new Interference(player);
        WildPitch,//(Player player) => new WildPitch(player);
        StolenBase,//(BaseRunner player, OnBase onBase) => new StolenBase(player, onBase);
        StealOfHome,
        StealOfHomeUnearned,
        OutStealing,//(Player player) => new OutStealing(player);
        DropThirdStrike,
        StrikeOutLooking,
        StrikeOutSwinging,
        CourtesyRunner
        //internal static InningEvent CopyFrom(UnfinshedAb result)
        //{
        //    var outsOnBases = result.Events.OfType<OutOnBases>().Count();
        //    InningEvent ev;
        //    if (outsOnBases >= 2)
        //        ev = TriplePlay;
        //    else if (outsOnBases == 1)
        //        ev = DoublePlay;
        //    else
        //        ev = GroundOut;
        //    foreach (var resev in result.Events)
        //        ev.AddEvent(resev);
        //    return ev.AddFielders(result);
        //   }


    }
}
