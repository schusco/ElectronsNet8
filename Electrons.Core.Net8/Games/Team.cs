using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Games
{
    public class Team
    {
        public Team()
        {
            _roster = new List<Player>();
            GamePitchers = new List<Pitcher>();
            _substitutions = new List<Substitution>();
            _order = new BattingOrder();
        }
        public event EventHandler PitcherChanged;
        public event EventHandler PlayerAdded;
        public Team(string name) : this() => Name = name;
        public Team(string name, List<Player> roster) : this(name) => _roster = roster;
        [JsonIgnore]
        public Player this[Position pos] => Lineup.FirstOrDefault(player => pos.Equals(player.Position));
        [JsonIgnore]
        public Player this[int spot] => _order.Order[spot - 1];
        public string Name { get; }
        public Field HomeField { get; private set; }
        public IList<Player> Lineup => _order?.Order ?? new List<Player>();
        [JsonIgnore]
        public IList<Player> AllPlayers => Lineup.Union(_substitutions.Select(s => s.NewPlayer)).ToList();
        [JsonIgnore]
        public IList<Player> AllBatters => Lineup.Union(_substitutions.Where(w => !(w is ReliefPitcher)).Select(s => s.NewPlayer)).ToList();
        public IEnumerable<Player> Roster => _roster.OrderBy(o => o.Number).ToList();
        [JsonIgnore]
        public List<Player> Bench => Lineup.Any() ? _roster.Except(Lineup).Except(Replaced).OrderBy(o => o.Number).ToList() : new List<Player>();
        [JsonIgnore]
        public List<Player> AvailablePitchers => _roster.Except(Replaced).OrderByDescending(o => o.IsPitcher).ToList();
        [JsonIgnore]
        public List<Player> Replaced => _substitutions.Select(s => s.Replaced).ToList();
        [JsonIgnore]
        public Player CurrentHitter => _order.CurrentSpot == 0 ? _order.Order.First() : _order.CurrentHitter;
        [JsonIgnore]
        public Pitcher CurrentPitcher => !GamePitchers.Any() ? null : GamePitchers.LastOrDefault(w => _substitutions.Any(a => (Pitcher)a.NewPlayer == w)) ?? GamePitchers.First();
        [JsonIgnore]
        public Pitcher StartingPitcher => GamePitchers.First();
        public override string ToString() => Name;
        public void SetBattingOrder(List<Player> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var spot = i + 1;
                _order.AddToLineup(spot, players[i]);
            }
            //if (players.Count >= 9 && _gameStarted)
            //    OrderIsSet = true;
        }
        public void SetHomeField(Field field) => HomeField = field;
        internal Player NextHitter(bool noAdvance) => _order.Next(noAdvance);

        private class BattingOrder
        {
            internal BattingOrder() : this(9) { }

            private BattingOrder(int spots)
            {
                _battingOrder = new Dictionary<int, Player>();
                _spotsInOrder = spots;
                //foreach (var spot in Enumerable.Range(1, _spotsInOrder))
                //    _battingOrder[spot] = Player.Blank;
            }
            internal BattingOrder(List<Player> players) : this(players.Count)
            {
                int i = 0;
                foreach (var player in players)
                    _battingOrder[++i] = player;
            }
            public int CurrentSpot { get; internal set; }
            public Player CurrentHitter => _battingOrder[CurrentSpot];
            internal Player AddToLineup(int spot, Player player)
            {
                if (spot >= 1)
                {
                    var replaced = _battingOrder.ContainsKey(spot) ? _battingOrder[spot] : null;
                    _battingOrder[spot] = player;
                    _spotsInOrder = _battingOrder.Count <= 9 ? 9 : _battingOrder.Count;
                    return replaced;
                }

                if (spot > _spotsInOrder)
                {
                    _battingOrder.Add(spot, player);
                    _spotsInOrder = _battingOrder.Count;
                }
                return null;
            }
            internal void RemoveFromLineup(Player player)
            {
                var spot = _battingOrder.Values.ToList().IndexOf(player) + 1;
                _battingOrder.Remove(spot);
            }
            public IList<Player> Order => _battingOrder.OrderBy(o => o.Key).Select(s => s.Value).ToList();
            internal Player Next(bool noAdvance)
            {
                if (noAdvance)
                    return CurrentHitter;
                CurrentSpot++;
                if (CurrentSpot > _spotsInOrder)
                    CurrentSpot = 1;
                return CurrentHitter;
            }
            internal void Sub(Player bench, int spot)
            {
                _battingOrder[spot] = bench;
            }
            internal void ReplaceWithUnknown(Player player)
            {
                var spot = _battingOrder.First(s => s.Value == player);
                _battingOrder[spot.Key] = Player.Unknown(spot.Key);
            }
            internal int LineupSpotOf(Player batter)
            {
                var kvp = _battingOrder.FirstOrDefault(f => f.Value == batter);
                return kvp.Key;
            }

            private readonly Dictionary<int, Player> _battingOrder;
            private int _spotsInOrder;
        }
        internal void FillOrder()
        {
            var battersInOrder = _order.Order.Count;
            for (int i = 9; i > battersInOrder; i--)
            {
                _order.Sub(Player.Unknown(i), i);
            }
            //foreach (var player in _order.Order)
            //{
            //    if (string.IsNullOrEmpty(player.FirstName))
            //    {
            //        _order.ReplaceWithUnknown(player);
            //OrderIsSet = true;
            //    }
            //}
        }
        public void SetRoster(IEnumerable<Player> roster) => _roster = roster.ToList();
        public Player AddToLineup(Player player, int? spot = null)
        {
            if (!spot.HasValue)
                spot = Lineup.Count(c => !string.IsNullOrEmpty(c.FirstName)) + 1;
            var replaced = _order.AddToLineup(spot.Value, player);
            //if (_order.Order.Count(c => !string.IsNullOrEmpty(c.FirstName)) >= 9)
            //    OrderIsSet = true;
            return replaced;
        }
        [JsonIgnore]
        public Pitcher PitcherOfRecord => GamePitchers.Single(s => s.IsPitcherOfRecord);
        public bool RemoveFromLineup(Player player)
        {
            if (!OrderIsSet)
                _order.RemoveFromLineup(player);
            return !OrderIsSet;
        }
        internal List<Pitcher> GamePitchers { get; }
        public void AddPlayer(Player player)
        {
            _roster.Add(player);
            OnPlayerAdded();
        }
        public void RemovePlayer(string lastName, int number)
        {
            var player = _roster.Where(s => s.LastName == lastName && s.Number == number).ToList();
            if (player.Count() == 0)
                return;
            foreach (var p in player)
                _roster.Remove(p);
        }
        private void OnPlayerAdded() => PlayerAdded?.Invoke(this, new EventArgs());
        internal static Team Load(XElement teamEl)
        {
            var team = new Team(teamEl.Element("Team").Attribute("name").Value);
            foreach (var player in teamEl.Descendants("Roster").Descendants("Player"))
                team._roster.Add(Player.Load(player));
            var lineup = new List<Player>();
            foreach (var player in teamEl.Descendants("Lineup").Descendants("Player"))
            {
                var p = Player.Load(player);
                lineup.Add(p);
                if (p.IsUnknown && string.IsNullOrEmpty(p.DisplayNumber))
                    p.DisplayNumber = lineup.Count.ToString();
            }
            team.SetBattingOrder(lineup);
            foreach (var player in teamEl.Descendants("Pitchers").Descendants("Player"))
            {
                var pitcher = (Pitcher)Player.Load(player);
                team.GamePitchers.Add(pitcher);
            }
            if (teamEl.Descendants().Any(a => a.Name == "HomeField"))
            {
                var homeFieldEl = teamEl.Descendants().Single(s => s.Name == "HomeField");
                team.SetHomeField(Field.Load(homeFieldEl));
            }
            return team;
        }
        [JsonIgnore]
        internal XElement Xml
        {
            get
            {
                var tm = new XElement("Team");
                tm.SetAttributeValue("name", Name);
                var xel = new XElement("Roster");
                foreach (var player in Roster)
                    xel.Add(player.Xml);
                tm.Add(xel);
                xel = new XElement("Lineup");
                foreach (var player in Lineup)
                    xel.Add(player.Xml);
                tm.Add(xel);
                xel = new XElement("Pitchers");
                foreach (var player in GamePitchers)
                    xel.Add(player.Xml);
                tm.Add(xel);
                if (!(HomeField is null))
                    tm.Add(HomeField.Xml);
                return tm;
            }
        }
        public bool OrderIsSet => _order.Order.Count >= 9 && _gameIsStarted;
        internal void SetNextHitter(Player batter)
        {
            int lineupSpot = 0;
            if (batter != null)
                lineupSpot = Lineup.IndexOf(batter);
            if (lineupSpot != -1)
                _order.CurrentSpot = ++lineupSpot;
        }
        internal Player UpdateLineup(Player batter, int spot) => _order.AddToLineup(spot, batter);
        internal Substitution ChangePitcher(Pitcher pitcher, bool saveSituation)
        {
            if (saveSituation)
                pitcher.IsSaveSituation();
            var sub = Substitution.ReliefPitcher((Player)pitcher, (Player)CurrentPitcher);
            _substitutions.Add(sub);
            GamePitchers.Add(pitcher);
            PitcherChanged?.Invoke(this, new EventArgs());
            return sub;
        }
        public void SetStartingPitcher(Pitcher starter)
        {
            if (GamePitchers.Any())
                GamePitchers.Clear();
            GamePitchers.Add(starter);
            starter.SetPitcherOfRecord();
            PitcherChanged?.Invoke(this, new EventArgs());
        }
        internal Substitution Substitute(Inning inning, Player bench, Player lineup)
        {
            var index = Lineup.IndexOf(lineup);
            bench.SetPosition(lineup.Position);
            _order.Sub(bench, index + 1);
            Substitution sub;
            if (inning.IsOnBase(lineup))
                sub = Substitution.PinchRunner(bench, lineup);
            else if (inning.CurrentAb.Batter == lineup)
                sub = Substitution.PinchHitter(bench, lineup);
            else
                sub = Substitution.Create(bench, lineup);
            _substitutions.Add(sub);
            return sub;
        }
        internal void AddSubstitutions(IEnumerable<Substitution> subs) => _substitutions.AddRange(subs);
        internal void OnScoreChanged(object sender, ScoreChangedEventArgs e)
        {
            var game = sender as BaseballGame;
            var isBatting = game.BattingTeam.Name == Name;
            var isHome = game.HomeTeam.Name == Name;

            var runs = e.AtBat.AdvancingRunners.OfType<RunScored>().ToList();
            var teamScore = isHome ? game.HomeScore : game.AwayScore;
            var oppoScore = isHome ? game.AwayScore : game.HomeScore;
            var runsInAb = runs.Sum(s => s.Runs);
            var prevScore = teamScore;
            var prevOppSc = oppoScore;
            if (isBatting)
                prevScore -= runsInAb;
            else
                prevOppSc -= runsInAb;
            var leading = TeamIsLeading(teamScore, oppoScore);
            var leadingPrev = TeamIsLeading(prevScore, prevOppSc);
            var leadChange = leading.GetValueOrDefault() != leadingPrev.GetValueOrDefault();
            var tmpScore = !isBatting ? prevScore : prevOppSc;
            var cngScore = isBatting ? prevScore : prevOppSc;
            if (!leading.HasValue || !leadingPrev.HasValue)
            {
                if (!runs.Any())
                    SetPitcherOfRecordTo(CurrentPitcher);
                else
                {
                    var pitcher = runs.LastOrDefault()?.ResponsiblePitcher;
                    if (pitcher is null)
                        SetPitcherOfRecordTo(CurrentPitcher);
                    else
                        SetPitcherOfRecordTo((Pitcher)pitcher);
                }
            }
            if (leadChange && leading.GetValueOrDefault())
                SetPitcherOfRecordTo(CurrentPitcher);
            else if (leadChange && !leading.GetValueOrDefault())
                Test(e.AtBat, tmpScore, cngScore);
            if (!leading.GetValueOrDefault() && leadingPrev.GetValueOrDefault())
                CurrentPitcher.BlewLead();
        }
        private void Test(AtBat ab, int tmpScore, int cngScore)
        {
            var runs = ab.AdvancingRunners.OfType<RunScored>().ToList();
            foreach (var run in runs)
            {
                cngScore += run.Runs;
                if (cngScore > tmpScore)
                {
                    SetPitcherOfRecordTo((Pitcher)run.ResponsiblePitcher);
                    break;
                }
            }
        }
        private bool? TeamIsLeading(int teamScore, int oppScore)
        {
            bool? leading = null;
            if (teamScore > oppScore)
                leading = true;
            else if (teamScore < oppScore)
                leading = false;
            return leading;
        }
        private void SetPitcherOfRecordTo(Pitcher pitcher)
        {
            foreach (var gamePitcher in GamePitchers)
            {
                if (gamePitcher == pitcher)
                    gamePitcher.SetPitcherOfRecord();
                else
                    gamePitcher.LetPitcherOffHook();
            }
        }
        public bool IsPlayerOnRoster(string text, int num)
        {
            var player = _roster.FirstOrDefault(s => s.LastName == text && s.Number == num);
            return !(player is null);
        }
        public Player GetPlayer(string text, int num) => _roster.SingleOrDefault(s => s.LastName == text.Trim() && s.Number == num);

        internal void GameStarted()
        {
            _gameIsStarted = true;
        }
        public static Team CreateWithUnknownRoster(string name) => new Team(name, Enumerable.Range(1, 25).Select(s => Player.Create(s, "Unknown", "Player")).ToList());
        public static Team Create(string name) => new Team(name);
        public int GetLineupSpotFor(Player player)
        {
            return _order.LineupSpotOf(player);
        }        

        private readonly List<Substitution> _substitutions;
        private List<Player> _roster;
        private BattingOrder _order;
        private bool _gameIsStarted;

    }
}
