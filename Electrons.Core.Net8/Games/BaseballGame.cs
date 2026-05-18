using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Games
{
    public class BaseballGame
    {
        private BaseballGame()
        {
            _innings = new Stack<Inning>();
            _navInnings = new Stack<Inning>();
            _gameDelays = new List<GameDelay>();
        }
        public BaseballGame(int inningLength) : this()
        {
            if (inningLength < 7)
                inningLength = 7;
            LastInningNumber = inningLength;
        }
        public DateTime GameDate { get; private set; }
        public DateTime? StartTime => _StartTime;
        public DateTime? EndTime => _EndTime;

        public event EventHandler DelayEnded;
        public event EventHandler DelayStart;
        public event EventHandler GameEnded;
        public event EventHandler InningUpdated;
        public event EventHandler<InningChangeEventArgs> InningStarted;
        public event EventHandler<InningChangeEventArgs> InningEnded;
        public event EventHandler PitchThrown;
        public event EventHandler<ScoreChangedEventArgs> ScoreChanged;
        [JsonIgnore]
        public AtBat CurrentAb => CurrentInning?.CurrentAb;
        [JsonIgnore]
        public TimeSpan DelayLength => _gameDelays.SumTimeSpan(x => x.LengthOfDelay);
        [JsonIgnore]
        public TimeSpan LengthOfGame
        {
            get
            {
                var end = _EndTime;
                if (!_StartTime.HasValue && !_EndTime.HasValue)
                    return new TimeSpan();
                if (_StartTime.HasValue && !_EndTime.HasValue)
                    end = DateTime.Now;
                if (!_gameDelays.Any())
                    return (end - _StartTime).Value;
                TimeSpan ts = new TimeSpan();
                DateTime workingTime = _StartTime.Value;
                foreach (var delay in _gameDelays)
                {
                    ts = ts.Add(delay.Start - workingTime);
                    workingTime = delay.End.Value;
                }
                ts = ts.Add(end.Value - workingTime);
                return ts;
            }
        }
        [JsonIgnore]
        public int InningLength => (CurrentInning?.Number ?? 1) < LastInningNumber ? LastInningNumber : CurrentInning.Number;
        public Inning StartGame()
        {
            if (IsStarted)
                return Innings.First();
            GameDate = DateTime.Today;
            if (!HomeTeam.OrderIsSet)
                HomeTeam.FillOrder();
            if (!AwayTeam.OrderIsSet)
                AwayTeam.FillOrder();
            if (!AwayTeam.GamePitchers.Any())
                AwayTeam.SetStartingPitcher((Pitcher)Player.Unknown(0));
            if (!HomeTeam.GamePitchers.Any())
                HomeTeam.SetStartingPitcher((Pitcher)Player.Unknown(0));
            _StartTime = DateTime.Now;
            HomeTeam.GameStarted();
            AwayTeam.GameStarted();
            return StartNextInning();
        }
        public Inning StartGame(DateTime gameDate, DateTime startTime)
        {
            var inning = StartGame();
            GameDate = gameDate;
            _StartTime = startTime;
            return inning;
        }
        public void DelayGame()
        {
            if (!(_gameDelays.LastOrDefault()?.End.HasValue ?? false))
            {
                _gameDelays.Add(new GameDelay { Start = DateTime.Now });
                DelayStart?.Invoke(this, new EventArgs());
            }
        }
        public void ResumeDelayedGame()
        {
            if (_gameDelays.Any() && !_gameDelays.Last().End.HasValue)
            {
                _gameDelays.Last().End = DateTime.Now;
                DelayEnded?.Invoke(this, new EventArgs());
            }
        }
        [JsonIgnore]
        public bool IsDelayed => !_gameDelays.LastOrDefault()?.End.HasValue ?? false;
        [JsonIgnore]
        public bool IsStarted => _StartTime.HasValue;
        [JsonIgnore]
        public bool IsGameOver => _EndTime.HasValue;
        public List<HStats> AwayTeamHitting => GetHittingStats(false);
        public List<HStats> HomeTeamHitting => GetHittingStats(true);
        public List<HStats> HomeHittingStats(Func<AtBat, bool> filter) => GetHittingStats(true, filter);
        public List<HStats> AwayHittingStats(Func<AtBat, bool> filter) => GetHittingStats(false, filter);
        public List<PStats> AwayTeamPitching => GetPitchingStats(false);
        public List<PStats> HomeTeamPitching => GetPitchingStats(true);
        private List<HStats> GetHittingStats(bool home, Func<AtBat, bool> filter = null)
        {
            var tb = home ? HalfInning.Bottom : HalfInning.Top;
            var team = home ? HomeTeam : AwayTeam;
            var innings = Innings.Where(w => w.Half == tb);
            var abs = innings.SelectMany(s => s.Events).ToList();
            if (filter != null)
                abs = abs.Where(filter).ToList();
            var hitStats = team.Lineup.Select(HStats.Create).ToList();
            foreach (var abList in abs.GroupBy(g => g.Batter))
            {
                var stat = hitStats.SingleOrDefault(s => s.Player == abList.Key);
                if ( stat is null)
                {
                    stat = HStats.Create(abList.Key);
                    hitStats.Add(stat);
                }
                stat.UpdateFromAbList(abList.ToList());
            }            
            var runEv = abs.SelectMany(s => s.AdvancingRunners).Distinct();
            var excluded = team.AllBatters.Except(abs.Select(s => s.Batter)).Union(runEv.Select(s => s.Player).Except(abs.Select(s => s.Batter))).Where(w => !(w is null));
            foreach (var excludedPlayer in excluded.Where(w => w.IsMemberOf(team)))
                hitStats.Add(HStats.Create(excludedPlayer));
            hitStats.ForEach(f => f.UpdateBaserunning(runEv.Where(w => f.Player == w.OriginalRunner && w.Player == f.Player
                                                                            || w.Player == f.Player && w.GetType().IsIn(BaseStats)
                                                                            || w.OriginalRunner == f.Player && !w.GetType().IsIn(BaseStats)), f.Player));
            return hitStats;
        }
        [JsonIgnore]
        public bool HasNoEventWildPitches
        {
            get
            {
                var abs = Innings.SelectMany(s => s.Events).ToList();
                foreach (var ab in abs)
                {
                    if (ab.Events.Any(a => a is RunnerAdvanceOnBatteryError))
                    {
                        var errorEvents = ab.Events.OfType<RunnerAdvanceOnBatteryError>();
                        if (errorEvents.Any(a => a.Events.Count() == 0))
                            return true;
                    }
                }
                return false;
            }
        }
        private List<PStats> GetPitchingStats(bool home)
        {
            var team = home ? HomeTeam : AwayTeam;
            var tb = home ? HalfInning.Top : HalfInning.Bottom;
            var innings = Innings.Where(w => w.Half == tb);
            var abs = innings.SelectMany(s => s.Events);
            var test = abs.GroupBy(g => g.Pitcher);
            var pitStats = abs.GroupBy(g => g.Pitcher).Select(PStats.Create).ToList();
            var runEv = Innings.SelectMany(s => s.AdvancingRunners).OfType<RunScored>();
            pitStats.ForEach(f => f.UpdateBaserunning(runEv.Where(w => w.ResponsiblePitcher == (Player)f.Player)));
            foreach (var stat in pitStats)
            {
                if (stat == pitStats.First())
                {
                    stat.SetAsStarter();
                    if (stat.Outs == 3 * LastInningNumber)
                        stat.SetAsCompleteGame();
                }
                if (IsGameOver)
                {
                    var pitcher = stat.GetPitcher(team.GamePitchers);
                    if (stat.Player == _winningPitcher)
                        stat.SetDecision(Decision.W);
                    if (stat.Player == _losingPitcher)
                        stat.SetDecision(Decision.L);
                    if (pitcher.EnteredInSaveSituation && pitcher.HeldLead)
                    {
                        if (Innings.SelectMany(s => s.Events).LastOrDefault()?.Pitcher == stat.Player)
                            stat.SetDecision(Decision.S);
                        else if (stat != pitStats.First() && stat.Outs > 0)
                            stat.SetDecision(Decision.H);
                    }
                    if (pitcher.EnteredInSaveSituation && !pitcher.HeldLead)
                    {
                        if (stat.Decision == Decision.W)
                            stat.SetDecision(Decision.BSW);
                        else if (stat.Decision == Decision.L)
                            stat.SetDecision(Decision.BSL);
                        else
                            stat.SetDecision(Decision.BS);
                    }
                }
            }
            if (pitStats.Count < team.GamePitchers.Count)
            {
                foreach (var player in team.GamePitchers.Skip(pitStats.Count))
                    pitStats.Add(PStats.Create((Player)player));
            }
            return pitStats;
        }
        internal Inning StartNextInning()
        {
            if (_navInnings.Any())
                return AddInning(_navInnings.Pop());
            Inning inning;
            if (!Innings.Any())
            {
                inning = Inning.TopOfOne(AwayTeam, HomeTeam.CurrentPitcher);
                return AddInning(inning);
            }
            var current = Innings.Last();
            HalfInning half = HalfInning.Bottom;
            var number = current.Number;
            var nextBattingTeam = HomeTeam;
            var nextPitchingTeam = AwayTeam;
            if (current.Half == HalfInning.Bottom)
            {
                half = HalfInning.Top;
                number++;
                nextBattingTeam = AwayTeam;
                nextPitchingTeam = HomeTeam;
            }
            var lastBattedInning = Innings.SingleOrDefault(w => w.Half == half && w.Number == number - 1);
            var noAdvance = !lastBattedInning?.CurrentAb?.IsFinished ?? false;
            inning = Inning.Create(number, half, nextBattingTeam, nextPitchingTeam.CurrentPitcher, noAdvance);
            return AddInning(inning);
        }
        public bool FinishAb()
        {
            var result = CurrentAb.Result;
            if (result is UnfinshedAb uab)
            {
                if (result.HasFielders)
                    CurrentAb.UpdateResult(uab);
                else
                    return false;
            }
            else if (result is FlyOut fo)
            {
                if (result.HasFielders)
                    CurrentAb.UpdateFlyoutResult(fo);
                else
                    return false;
            }
            var currentAbFinished = CurrentAb.Finish(CurrentInning);
            if (!currentAbFinished && !CurrentInning.InningIsFinished)
                CurrentInning.NextBatter();
            if (!currentAbFinished && CurrentInning.InningIsFinished)
                StartNextInning();
            return true;
        }
        private Inning AddInning(Inning inning)
        {
            _innings.Push(inning);
            inning.InningUpdated += Inning_InningUpdated;
            inning.InningEnded += Inning_InningEnded;
            inning.RunScored += Inning_RunScored;
            InningStarted?.Invoke(this, new InningChangeEventArgs(inning));
            return inning;
        }
        internal void PushInning(Inning inning)
        {
            _innings.Push(inning);
            inning.InningUpdated += Inning_InningUpdated;
            inning.InningEnded += Inning_InningEnded;
        }
        internal void Inning_RunScored(object sender, EventArgs e)
        {
            ScoreChanged?.Invoke(this, new ScoreChangedEventArgs(sender as AtBat));
        }
        internal void Inning_ReliefPitcherEntered(object sender, InningEventArgs e)
        {
            var rp = e.Event as ReliefPitcher;
            if (FieldingTeam.IsPlayerOnRoster(rp.NewPlayer.LastName, rp.NewPlayer.Number))
            {
                var newPitcher = FieldingTeam.GamePitchers.Single(s => s == (Pitcher)rp.NewPlayer);
                var isSaveSituation = CurrentInning.Number >= 5 && IsSaveSituationFor(FieldingTeam);
                if (isSaveSituation)
                    newPitcher.IsSaveSituation();
            }
        }
        private void Inning_InningEnded(object sender, EventArgs e)
        {
            if (!IsGameOver)
                CheckForEndOfGame();
            InningEnded?.Invoke(this, new InningChangeEventArgs(sender as Inning));
            if (!IsGameOver)
                StartNextInning();
        }
        public bool AddScoring()
        {
            var scoringAdded = CurrentAb.AddScoring();
            if (!scoringAdded)
            {
                var ab = CurrentInning.Events.OrderByDescending(o => o.Sequence).Skip(1).FirstOrDefault();
                ab?.AddScoring();
            }

            if (CurrentAb.IsFinished && !IsGameOver)
                CurrentInning.NextBatter();
            if (CurrentInning.Outs == 3)
                StartNextInning();
            return scoringAdded;
        }
        [JsonIgnore]
        public Team BattingTeam => CurrentInning.Half == HalfInning.Top ? AwayTeam : HomeTeam;
        [JsonIgnore]
        public Team FieldingTeam => CurrentInning.Half == HalfInning.Top ? HomeTeam : AwayTeam;
        [JsonIgnore]
        public Team LeadingTeam => HomeScore > AwayScore ? HomeTeam : AwayScore > HomeScore ? AwayTeam : null;
        private void Inning_InningUpdated(object sender, EventArgs e)
        {
            var inning = sender as Inning;
            if (inning.Number >= LastInningNumber)
                CheckForEndOfGame();
            InningUpdated?.Invoke(this, e);
        }
        private void CheckForEndOfGame(bool end = false)
        {
            if (CurrentInning.Number < LastInningNumber && !end)
                return;
            if (CurrentInning.Half == HalfInning.Bottom)
            {
                if (CurrentInning.InningIsFinished && HomeScore != AwayScore)
                    OnGameEnded();
                else if (HomeScore > AwayScore)
                    OnGameEnded(true);
            }
            else
            {
                if (CurrentInning.InningIsFinished && HomeScore > AwayScore)
                    OnGameEnded();
            }
        }
        public Substitution ChangePitcher(Team team, Player player)
        {
            var reliefPitcher = (Pitcher)player;
            var isSaveSituation = IsSaveSituationFor(team);
            var sub = team.ChangePitcher(reliefPitcher, isSaveSituation);
            if (!CurrentAb.IsFinished)
                CurrentAb.SetPitcher(player);
            return sub;
        }
        public Substitution Substitute(Team team, Player bench, Player lineup) => team.Substitute(CurrentInning, bench, lineup);
        private void OnGameEnded(bool walkOff = false, DateTime? endTime = null)
        {
            _EndTime = endTime ?? DateTime.Now;
            if (walkOff)
                CurrentInning.EndedOnWalkOff();
            SetPitchersOfRecord();
            GameEnded?.Invoke(this, new EventArgs());
        }
        public Player WinningPitcher => (Player)_winningPitcher;
        public Player LosingPitcher => (Player)_losingPitcher;
        [JsonIgnore]
        public Player SaveAwardedTo
        {
            get
            {
                if (HomeScore == AwayScore)
                    return null;
                var winningTeam = HomeScore > AwayScore ? HomeTeam : AwayTeam;
                if (winningTeam.CurrentPitcher.EarnedSave)
                    return (Player)winningTeam.CurrentPitcher;
                return null;
            }
        }
        private void SetPitchersOfRecord()
        {
            if (HomeScore == AwayScore)
                return;
            var winningTeam = HomeScore > AwayScore ? HomeTeam : AwayTeam;
            var losingTeam = HomeScore > AwayScore ? AwayTeam : HomeTeam;
            if (_winningPitcher is null)
                _winningPitcher = winningTeam.PitcherOfRecord;
            _losingPitcher = losingTeam.PitcherOfRecord;
            if (winningTeam.CurrentPitcher.EnteredInSaveSituation && winningTeam.CurrentPitcher.HeldLead)
            {
                if (winningTeam.CurrentPitcher != _winningPitcher)
                    winningTeam.CurrentPitcher.AwardSave();
            }
            else if (Innings.SelectMany(s => s.Events.Where(w => w.Pitcher == winningTeam.CurrentPitcher)).Sum(s => s.Outs) >= 9)
                if (winningTeam.CurrentPitcher != winningTeam.StartingPitcher && winningTeam.CurrentPitcher != _winningPitcher)
                    winningTeam.CurrentPitcher.AwardSave();
        }
        public void SetWinningPitcher(Pitcher pitcher)
        {
            _winningPitcher = pitcher;
        }
        private bool IsSaveSituationFor(Team team)
        {
            var saveSituation = false;
            var home = team == HomeTeam;
            var difference = home ? HomeScore - AwayScore : AwayScore - HomeScore;
            if (difference > 0 && (CurrentInning?.Number ?? 1) >= 5)
            {
                if (difference <= 3)
                    saveSituation = true;
                else if (2 + CurrentInning.CurrentRunners.Count >= difference)
                    saveSituation = true;
            }
            return saveSituation;
        }
        [JsonIgnore]
        public Inning CurrentInning => Innings.LastOrDefault();
        [JsonPropertyName("current_ab")]
        public CurrentAbData CurrentAbInfo => CurrentAbData.Create(CurrentInning);
        [JsonPropertyName("defense")]
        public IDictionary<int, string> Defense
        {
            get
            {
                var dict = new Dictionary<int, string>();
                foreach (var player in FieldingTeam.Lineup)
                    dict[player.Position.PositionNumber] = player.LastName;
                dict[1] = FieldingTeam.CurrentPitcher.LastName;
                return dict;
            }
        }
        [JsonIgnore]
        public string LengthOfGameString
        {
            get
            {
                if (!_StartTime.HasValue && !_EndTime.HasValue)
                    return "No game in progress";
                var timeString = LengthOfGame.TimeLength();
                if (_EndTime.HasValue)
                    return timeString;
                return $"{timeString} (in progress)";
            }
        }
        [JsonPropertyName("home_score")]
        public int HomeScore => Innings.Where(w => w.Half == HalfInning.Bottom).Sum(s => s.Runs);
        [JsonPropertyName("away_score")]
        public int AwayScore => Innings.Where(w => w.Half == HalfInning.Top).Sum(s => s.Runs);
        [JsonPropertyName("home_hits")]
        public int HomeHits => Innings.Where(w => w.Half == HalfInning.Bottom).Sum(s => s.Hits);
        [JsonPropertyName("away_hits")]
        public int AwayHits => Innings.Where(w => w.Half == HalfInning.Top).Sum(s => s.Hits);
        [JsonPropertyName("home_errors")]
        public int HomeErrors => Innings.Where(w => w.Half == HalfInning.Top).Sum(s => s.Errors);
        [JsonPropertyName("away_errors")]
        public int AwayErrors => Innings.Where(w => w.Half == HalfInning.Bottom).Sum(s => s.Errors);
        public List<ScoringPlay> ScoringPlays
        {
            get
            {
                var returnList = new List<ScoringPlay>();
                var homeScore = 0;
                var awayScore = 0;
                var innings = Innings.Where(w => w.Runs > 0);
                foreach (var scoringInning in innings)
                {
                    var scoringAbs = scoringInning.Events.Where(w => w.Runs > 0);
                    foreach (var scoringPlay in scoringAbs)
                    {
                        if (scoringInning.Half == HalfInning.Bottom)
                            homeScore += scoringPlay.Runs;
                        else
                            awayScore += scoringPlay.Runs;
                        var teamName = scoringInning.Half == HalfInning.Top ? AwayTeam.Name : HomeTeam.Name;
                        var play = ScoringPlay.Create(scoringPlay.ScoreText, teamName, scoringInning.Number, homeScore, awayScore);

                        returnList.Add(play);
                    }
                }
                return returnList;
            }
        }

        public List<InningSummary> GameSummary
        {
            get
            {
                var returnList = new List<InningSummary>();
                foreach (var inning in Innings)
                    returnList.Add(InningSummary.Create(inning));
                return returnList;

            }
        }
        public LineScore GameLineScore => LineScore.Create(HomeTeam.Name, AwayTeam.Name, _innings.ToList());
        [JsonIgnore]
        public Team AwayTeam
        {
            get => awayTeam;
            private set
            {
                awayTeam = value;
                awayTeam.PitcherChanged += PitcherAdded;
                ScoreChanged += awayTeam.OnScoreChanged;
            }
        }
        [JsonIgnore]
        public Team HomeTeam
        {
            get => homeTeam;
            private set
            {
                homeTeam = value;
                homeTeam.PitcherChanged += PitcherAdded;
                ScoreChanged += homeTeam.OnScoreChanged;
            }
        }
        [JsonPropertyName("home_risp")]
        public Risp HomeRisp
        {
            get
            {
                var stats = HomeHittingStats(AtBat.WithRispFilter);
                return new Risp(stats.Sum(s => s.H), stats.Sum(s => s.AB));
            }
        }
        [JsonPropertyName("away_risp")]
        public Risp AwayRisp
        {
            get
            {
                var stats = AwayHittingStats(AtBat.WithRispFilter);
                return new Risp(stats.Sum(s => s.H), stats.Sum(s => s.AB));
            }
        }
        private void PitcherAdded(object sender, EventArgs e)
        {
            if (!IsStarted)
                return;
            var team = sender as Team;
            if (team.Equals(homeTeam) && CurrentInning.Half == HalfInning.Top)
                CurrentInning.SetPitcher(team.CurrentPitcher);
            else if (team.Equals(awayTeam) && CurrentInning.Half == HalfInning.Bottom)
                CurrentInning.SetPitcher(team.CurrentPitcher);
        }
        public void SetAwayTeam(Team team)
        {
            if (AwayTeam != null)
                throw new BaseballGameException("Away team already defined");
            AwayTeam = team;
        }
        public void SetHomeTeam(Team team)
        {
            if (HomeTeam != null)
                throw new BaseballGameException("Home team already defined");
            HomeTeam = team;
        }
        [JsonIgnore]
        public IOrderedEnumerable<Inning> Innings => _innings.OrderBy(o => o.Number).ThenBy(o => (int)o.Half);
        public void SaveAs(string fname)
        {
            Xml.Save(fname);
        }
        [JsonIgnore]
        public XDocument Xml
        {
            get
            {
                XDocument xdoc = new XDocument();
                var rootEl = new XElement("BaseballGame");
                xdoc.Add(rootEl);
                rootEl.Add(new XElement("GameLength", LastInningNumber));
                rootEl.Add(new XElement("GameDate", GameDate), new XElement("StartTime", _StartTime), new XElement("EndTime", _EndTime));
                rootEl.Add(new XElement("HomeTeam", HomeTeam.Xml), new XElement("AwayTeam", AwayTeam.Xml));
                if (!(_winningPitcher is null))
                    rootEl.Add(new XElement("WinningPitcher", _winningPitcher.Xml));
                rootEl.Add(new XElement("GameDelays", _gameDelays.Select(s => s.Xml)));
                foreach (var inning in Innings)
                    rootEl.Add(inning.Xml);
                return xdoc;
            }
        }
        [JsonIgnore]
        public static Type[] BaseStats => new[] { typeof(StolenBase), typeof(OutStealing) };
        public static BaseballGame Load(string fileName) => Load(XDocument.Load(fileName));
        public void SetCurrentAbFieldLocation(FieldLocation loc)
        {
            CurrentAb.Result.SetFieldLocation(loc);
        }
        public static BaseballGame Load(XDocument xdoc)
        {
            BaseballGame game;
            try
            {
                int innings = int.Parse(xdoc.Descendants().SingleOrDefault(s => s.Name == "GameLength")?.Value ?? "7");
                game = new BaseballGame(innings)
                {
                    GameDate = DateTime.Parse(xdoc.Descendants().Single(s => s.Name == "GameDate").Value),
                };
                var startEl = xdoc.Descendants().Single(s => s.Name == "StartTime").Value;
                if (!string.IsNullOrEmpty(startEl))
                    game._StartTime = DateTime.Parse(startEl);
                var endEl = xdoc.Descendants().Single(s => s.Name == "EndTime").Value;
                if (!string.IsNullOrEmpty(endEl))
                    game._EndTime = DateTime.Parse(endEl);
                var delayEl = xdoc.Descendants().SingleOrDefault(s => s.Name == "GameDelays");
                if (!(delayEl is null))
                    foreach (var gd in delayEl.Descendants().Where(w => w.Name == "GameDelay"))
                        game._gameDelays.Add(GameDelay.Load(gd));
                game.HomeTeam = Team.Load(xdoc.Descendants().Single(s => s.Name == "HomeTeam"));
                game.AwayTeam = Team.Load(xdoc.Descendants().Single(s => s.Name == "AwayTeam"));
                if (xdoc.Descendants().SingleOrDefault(s => s.Name == "WinningPitcher") != null)
                {
                    var xel = xdoc.Descendants().Single(s => s.Name == "WinningPitcher");
                    var wp = (Pitcher)Player.Load(xel.Descendants().First());
                    game.SetWinningPitcher(wp);
                }
                foreach (var inningEl in xdoc.Descendants("Inning"))
                {
                    var half = (HalfInning)Enum.Parse(typeof(HalfInning), inningEl.Attribute("half").Value);
                    var team = half == HalfInning.Top ? game.AwayTeam : game.HomeTeam;
                    var opposition = half == HalfInning.Top ? game.HomeTeam : game.AwayTeam;
                    var inning = Inning.Load(inningEl, game, team, opposition);
                }
                game.HomeTeam.SetNextHitter(game.Innings.Where(l => l.Half == HalfInning.Bottom).SelectMany(s => s.Events).LastOrDefault()?.Batter);
                game.AwayTeam.SetNextHitter(game.Innings.Where(w => w.Half == HalfInning.Top).SelectMany(s => s.Events).LastOrDefault()?.Batter);
                if (game.IsGameOver)
                    game.SetPitchersOfRecord();
                if (game.IsStarted)
                {
                    game.homeTeam.GameStarted();
                    game.awayTeam.GameStarted();
                }
            }
            catch (Exception ex)
            {
                throw new BaseballGameException("Unable to load game", ex);
            }
            return game;
        }
        public Inning GetInning(int number, HalfInning half) => Innings.Single(w => w.Half == half && w.Number == number);
        public Inning GetInningByIndex(int index) => Innings.ToList()[index];

        private class GameDelay
        {
            public DateTime Start { get; set; }
            public DateTime? End { get; set; }
            public TimeSpan LengthOfDelay => End.HasValue ? End.Value - Start : DateTime.Now - Start;
            public XElement Xml => new XElement("GameDelay", new XElement("DelayStart", Start.ToString()), new XElement("DelayEnd", End.HasValue ? End.ToString() : ""));
            internal static GameDelay Load(XElement delayEl)
            {
                var delay = new GameDelay
                {
                    Start = DateTime.Parse(delayEl.Descendants().Single(s => s.Name == "DelayStart").Value.ToString())
                };
                var endEl = delayEl.Descendants().SingleOrDefault(s => s.Name == "DelayEnd");
                if (!(endEl is null))
                    delay.End = DateTime.Parse(endEl.Value.ToString());
                return delay;
            }

            public override string ToString() => LengthOfDelay.TimeLength();
        }

        public IList<RunningEvent> ForceRunners(bool reachedOnError = false, bool chargeRunAsEarned = true) => CurrentInning.ForceRunners(reachedOnError, chargeRunAsEarned).ToList();
        public IList<RunningEvent> AdvanceAllRunners(int v, bool includeBatter = true, bool reachedOnError = false, bool chargeRunAsEarned = true)
        {
            var advances = CurrentInning.AdvanceAllRunners(v, includeBatter, reachedOnError, chargeRunAsEarned).ToList();
            if (advances.Sum(s => s.Runs) > 0)
                ScoreChanged?.Invoke(this, new ScoreChangedEventArgs(CurrentAb, advances));
            return advances;
        }
        public RunningEvent ScoreRunner(BaseRunner runner, AdvanceReason reason, bool chargeAsEarned = true)
        {
            RemoveRunningEvent(OnBase.Third, runner.Runner);
            return CurrentInning.ScoreRunner(runner, reason == AdvanceReason.Error, chargeAsEarned);
        }
        public RunningEvent AdvanceRunner(BaseRunner runner, OnBase? onBase = null, AdvanceReason reason = AdvanceReason.Ab)
        {
            var currentBase = GetCurrentBase(runner);
            if (onBase is null)
            {
                if (currentBase == OnBase.First)
                    onBase = OnBase.Second;
                else if (currentBase == OnBase.Second)
                    onBase = OnBase.Third;
                else if (currentBase == OnBase.Third)
                    onBase = OnBase.None;
                else
                    throw new ArgumentException("Invalid base for runner to advance");
            }
            var rev = CurrentInning.AdvanceRunner(runner, onBase.Value, reason);
            RemoveRunningEvent(currentBase, runner.Runner);
            return rev;
        }
        public AtBatResult PreviousInning()
        {
            if (!_innings.Any() || _innings.Count == 1)
                return CurrentAb.Result;
            var inning = _innings.Pop();
            _navInnings.Push(inning);
            UpdateBatter();
            return CurrentAb.Result;
        }
        public AtBatResult NextInning()
        {
            if (!_navInnings.Any())
                return CurrentAb.Result;
            var inning = _navInnings.Pop();
            _innings.Push(inning);
            UpdateBatter();
            return CurrentAb.Result;
        }
        public AtBatResult PreviousAtBat()
        {
            CurrentInning.PreviousAtBat();
            return CurrentAb.Result;
        }
        public AtBatResult NextAtBat()
        {
            CurrentInning.NextAtBat();
            return CurrentAb.Result;
        }
        private void UpdateBatter()
        {
            var team = CurrentInning.Half == HalfInning.Top ? AwayTeam : HomeTeam;
            var lastHitter = CurrentInning.Events.Last().Batter;
            var lineupSpot = team.Lineup.IndexOf(lastHitter) + 1;
            var nextSpot = lineupSpot == team.Lineup.Count ? 1 : lineupSpot + 1;
            var nextHitter = team[nextSpot];
            team.SetNextHitter(nextHitter);
        }
        public void UpdateCurrentAbResult(AB ab, IList<RunningEvent> advances = null)
        {
            CurrentAb.UpdateResult(ab, advances);
        }
        public void MoveCurrent()
        {
            var cnt = _navInnings.Count;
            for (int i = 0; i < cnt; i++)
            {
                var inning = _navInnings.Pop();
                inning.MoveCurrent();
                _innings.Push(inning);
            }
        }
        public void AddRunnerAdvances(AB ab, IList<RunningEvent> advances)
        {
            if (!(InningEvent.GetInstance(ab.ToString()) is RunnerAdvanceOnBatteryError ev))
                throw new ArgumentException("invalid entry");
            ev.SetPitcher((Player)CurrentInning.CurrentPitcher);
            foreach (var advance in advances)
                ev.AddEvent(advance);
            CurrentAb.AddEvent(ev);
        }
        public void AddEventToAb(InningEvent ev)
        {
            if (ev is AtBatResult)
                throw new ArgumentException("At Bat Results cannot be added directly At Bats");
            else if (ev is RunningEvent rev)
            {
                RemoveRunningEvent(rev.AdvanceTo, rev.Player);
                if (CurrentAb.Result is UnfinshedAb && !CurrentAb.Result.HasFielders)
                {
                    if (CurrentAb.Events.Any(a => a.IsScoringRequired()))
                    {
                        var scEv = CurrentAb.Events.First(f => f.IsScoringRequired());
                        scEv.AddEvent(ev);
                    }
                    else
                        CurrentAb.AddEvent(ev);
                }
                else
                {
                    if (CurrentAb.Result.Events.Any(a => a.IsScoringRequired() && !(a is OutOnBases)))
                    {
                        var scEv = CurrentAb.Result.Events.First(f => f.IsScoringRequired());
                        scEv.AddEvent(ev);
                    }
                    else
                        CurrentAb.Result.AddEvent(rev);
                }
            }
            else if (ev is Pitch)
            {
                CurrentAb.AddEvent(ev);
                PitchThrown?.Invoke(this, new EventArgs());
            }
            else
                CurrentAb.AddEvent(ev);
        }
        public void AddEventToAb(AB ab, Player pitcher, IList<RunningEvent> advancingRunners) => CurrentAb.AddEvent(ab, pitcher, advancingRunners);
        public void AddEventToAb(AB ab, BaseRunner runner, OnBase nextBase) => CurrentAb.AddEvent(ab, runner, nextBase);
        private RunningEvent RemoveRunningEvent(OnBase onBase, Player runner)
        {
            if (CurrentAb.AdvancingRunners.Any(a => a.AdvanceTo == onBase && a.Player == runner))
            {
                var advance = CurrentAb.AdvancingRunners.Single(s => s.AdvanceTo == onBase && s.Player == runner);
                if (!advance.GetType().IsIn(typeof(StolenBase), typeof(StealOfHome), typeof(AdvancedOnError)))
                {
                    //CurrentAb.RemoveRunningEvent(advance);
                    return advance;
                }
            }
            return null;
        }
        private RunningEvent GetRunningEvent(OnBase onBase, Player runner)
        {
            if (CurrentAb.AdvancingRunners.Any(a => a.AdvanceTo == onBase && a.Player == runner))
                return CurrentAb.AdvancingRunners.Single(s => s.AdvanceTo == onBase && s.Player == runner);
            return null;
        }
        private OnBase GetCurrentBase(BaseRunner runner)
        {
            var kvp = CurrentInning.Runners.Single(s => s.Value.Equals(runner));
            return kvp.Key;
        }
        public RunningEvent ReturnRunner(OnBase onBase)
        {
            var runner = CurrentInning.CurrentRunners[onBase];
            var advance = GetRunningEvent(onBase, runner.Runner);
            if (!CurrentAb.Result.RemoveRunningEvent(advance))
                CurrentAb.RemoveRunningEvent(advance);
            return advance;
        }
        public void SetRunsBattedInForAb(int totalRbis)
        {
            CurrentAb?.SetRunsBattedIn(totalRbis);
        }
        public void AddRunnerOutEvent(BaseRunner runner, OnBase onBase)
        {
            if (CurrentAb.Result.FinishedAb || CurrentAb.Result.HasFielders)
                CurrentAb.Result.AddEvent(new OutOnBases(runner, onBase));
            else
                CurrentAb.AddEvent(new PickOff(runner, onBase));
        }
        public void UndoScoring()
        {
            CurrentAb.UndoScoring(CurrentInning);
        }
        public void EndGame(DateTime? endTime = null)
        {
            if (CurrentAb?.Result is null || CurrentAb.Result.GetType() == typeof(UnfinshedAb))
                CurrentInning?.RemoveLastAb();
            OnGameEnded(endTime: endTime);
        }
        public void UpdateUnknownHitter(Player newBatter, HalfInning half)
        {
            BattingTeam.UpdateLineup(newBatter, CurrentInning.SpotInLineup);
            var unknownHitter = CurrentInning.CurrentAb.Batter;
            CurrentInning.CurrentAb.SetBatter(newBatter);
            var previousAbs = Innings.Where(w => w.Half == half).SelectMany(s => s.Events).Where(w => w.Batter == unknownHitter);
            foreach (var previousAb in previousAbs)
                previousAb.SetBatter(newBatter);
        }
        public void UpdatePlayer(Player replaced, Player newPlayer)
        {
            foreach (var inning in Innings)
            {
                foreach (var ab in inning.Events.Where(w => w.Pitcher != null && w.Pitcher.Equals(replaced)))
                    ab.SetPitcher(newPlayer);
                foreach (var ab in inning.Events.Where(w => w.Batter != null && w.Batter.Equals(replaced)))
                    ab.SetBatter(newPlayer);
                foreach (var ab in inning.Events.Where(w => w.AdvancingRunners.Any(a => a.Player.Equals(replaced))))
                {
                    var advances = ab.AdvancingRunners.Where(w => w.Player.Equals(replaced)).ToList();
                    foreach (var advance in advances)
                        advance.SetPlayer(newPlayer);
                }
            }
        }
        public override string ToString() => $"{AwayTeam?.Name} {AwayScore}, {HomeTeam?.Name} {HomeScore} {(IsGameOver ? "Final" : CurrentInning?.ToString())}";

        public void SetGameEndTime(DateTime? endDateTime)
        {
            _EndTime = endDateTime ?? DateTime.Now;
        }

        private readonly Stack<Inning> _innings;
        private readonly Stack<Inning> _navInnings;
        private Team homeTeam;
        private Team awayTeam;
        private DateTime? _StartTime;
        private DateTime? _EndTime;
        private readonly List<GameDelay> _gameDelays;
        private Pitcher _winningPitcher;
        private Pitcher _losingPitcher;
        internal readonly int LastInningNumber;
    }
    public class Risp
    {
        public Risp(int hits, int abs)
        {
            Abs = abs;
            Hits = hits;
        }
        [JsonPropertyName("abs")]
        public int Abs { get; set; }
        [JsonPropertyName("hits")]
        public int Hits { get; set; }
    }
    public class ScoringPlay
    {
        public int HomeScore { get; internal set; }
        public int AwayScore { get; internal set; }
        public int InningNumber { get; internal set; }
        public string Team { get; set; }
        public string Play { get; set; }
        internal static ScoringPlay Create(string text, string team, int inningNumber, int homeScore, int awayScore)
        {
            return new ScoringPlay
            {
                Team = team,
                HomeScore = homeScore,
                AwayScore = awayScore,
                InningNumber = inningNumber,
                Play = text
            };

        }
        public override string ToString() => $"{Play} {HomeScore} - {AwayScore}";
    }
    public class LineScore
    {
        public TeamLine HomeLine { get; set; }
        public TeamLine AwayLine { get; set; }

        internal static LineScore Create(string homeTeam, string awayTeam, List<Inning> innings)
        {
            return new LineScore
            {
                HomeLine = TeamLine.Create(homeTeam, innings.Where(w => w.Half == HalfInning.Bottom)),
                AwayLine = TeamLine.Create(awayTeam, innings.Where(w => w.Half == HalfInning.Top))
            };
        }
    }
    public class TeamLine
    {
        public string Name { get; set; }
        public List<InningLine> InningLines { get; set; }
        internal static TeamLine Create(string homeTeam, IEnumerable<Inning> innings)
        {
            return new TeamLine
            {
                Name = homeTeam,
                InningLines = innings.Select(InningLine.Create).ToList()
            };
        }
    }
    public class InningLine
    {
        public int Runs { get; set; }
        public int Hits { get; set; }
        public int Errors { get; set; }
        public int Number { get; set; }

        internal static InningLine Create(Inning source)
        {
            return new InningLine
            {
                Number = source.Number,
                Runs = source.Runs,
                Hits = source.Hits,
                Errors = source.Errors,
            };
        }
    }
    public class InningSummary
    {
        public string Description { get; set; }
        public string Summary { get; set; }
        public List<InningPlay> Events { get; set; }
        internal static InningSummary Create(Inning inning)
        {
            return new InningSummary
            {
                Description = inning.ToString(),
                Summary = inning.InningSummary,
                Events = inning.Events.Select(InningPlay.Create).ToList()
            };
        }
    }
    public class InningPlay
    {
        public string EventText { get; set; }
        public bool ScoringPlay { get; set; }
        internal static InningPlay Create(AtBat arg)
        {
            return new InningPlay
            {
                EventText = arg.ToString(),
                ScoringPlay = arg.AdvancingRunners.Any(a => a.Runs > 0)
            };
        }
    }
    public class CurrentAbData
    {
        [JsonPropertyName("balls")]
        public int Balls { get; private set; }
        [JsonPropertyName("strikes")]
        public int Strikes { get; private set; }
        [JsonPropertyName("outs")]
        public int Outs { get; private set; }
        [JsonPropertyName("pitches")]
        public IEnumerable<string> Pitches { get; private set; }
        [JsonPropertyName("inning_number")]
        public int InningNumber { get; private set; }
        [JsonPropertyName("inning_half")]
        public string InningHalf { get; private set; }
        [JsonPropertyName("on_first")]
        public string OnFirst { get; private set; }
        [JsonPropertyName("on_second")]
        public string OnSecond { get; private set; }
        [JsonPropertyName("on_third")]
        public string OnThird { get; private set; }

        internal static CurrentAbData Create(Inning currentInning)
        {
            return new CurrentAbData
            {
                Balls = currentInning.CurrentAb.Balls,
                Strikes = currentInning.CurrentAb.Strikes,
                Outs = currentInning.CurrentAb.Outs,
                Pitches = currentInning.CurrentAb.Pitches.Select(s => s.ToString()),
                InningNumber = currentInning.Number,
                InningHalf = currentInning.Half.ToString(),
                OnFirst = currentInning.CurrentRunners.OnFirst?.Runner?.DisplayName,
                OnSecond = currentInning.CurrentRunners.OnSecond?.Runner?.DisplayName,
                OnThird = currentInning.CurrentRunners.OnThird?.Runner?.DisplayName,
            };
        }
    }
}
