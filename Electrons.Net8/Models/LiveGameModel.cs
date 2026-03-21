using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Games;
using Electrons.Core.Net8.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class LiveGameModel
    {
        public LiveGameModel()
        {
            ScoringPlays = new List<ScoringPlayModel>();
            Defense = new Dictionary<Position, string>();
            Innings = new List<InningModel>();
            HomeInnings = new List<int>();
            AwayInnings = [];
            HomeBox = BoxScore.Blank;
            AwayBox = BoxScore.Blank;
            PreviousAbs = new List<string>();
            Pitches = new List<string>();
            Defense = new Dictionary<Position, string>();
            foreach (var position in Position.All)
                Defense.Add(position, "");
        }
        public LiveGameModel(BaseballGame fullGame, DateTime gameDate, Location location, List<HittingStatsRow> seasonStats = null) : this()
        {
            Fill(fullGame, gameDate, location, seasonStats);
            HomeBox = BoxScore.Create(fullGame.HomeTeamHitting, fullGame.HomeTeamPitching, fullGame.HomeTeam, seasonStats);
            AwayBox = BoxScore.Create(fullGame.AwayTeamHitting, fullGame.AwayTeamPitching, fullGame.AwayTeam, seasonStats);
        }
        private void Fill(BaseballGame fullGame, DateTime gameDate, Location location, List<HittingStatsRow> seasonStats = null)
        {
            Title = $"{fullGame.AwayTeam} @ {fullGame.HomeTeam}, {fullGame.GameDate.ToShortDateString()}";
            GameIsOver = fullGame.IsGameOver;
            IsStarted = fullGame.IsStarted;
            GameTime = gameDate.ToShortTimeString();
            HomeScore = fullGame.HomeScore;
            AwayScore = fullGame.AwayScore;
            HomeHits = fullGame.HomeHits;
            HomeErrors = fullGame.HomeErrors;
            AwayHits = fullGame.AwayHits;
            AwayErrors = fullGame.AwayErrors;
            HomeName = fullGame.HomeTeam.Name;
            AwayName = fullGame.AwayTeam.Name;
            LocationText = location?.Field ?? "";
            CityText = location?.CityAndState ?? "";

            AwayLogo = fullGame.AwayTeam.Name.GetLogo();
            HomeLogo = fullGame.HomeTeam.Name.GetLogo();
            if (!fullGame.IsStarted)
                return;
            if (fullGame.CurrentInning.Half == HalfInning.Top)
            {
                PitcherLogo = HomeLogo;
                HitterLogo = AwayLogo;
            }
            else
            {
                PitcherLogo = AwayLogo;
                HitterLogo = HomeLogo;
            }
            InningNumber = fullGame.CurrentInning.Number;
            InningHalf = fullGame.CurrentInning.Half;
            Balls = fullGame.CurrentAb.Balls;
            Strikes = fullGame.CurrentAb.Strikes;
            Outs = fullGame.CurrentInning.Outs;
            TotalInnings = fullGame.InningLength;
            foreach (var inning in fullGame.Innings)
            {
                if (inning.Half == HalfInning.Top)
                    AwayInnings.Add(inning.Runs);
                else
                    HomeInnings.Add(inning.Runs);
            }
            foreach (var player in fullGame.FieldingTeam.Lineup)
                Defense[player.Position] = player.LastName;
            Defense[Position.P] = fullGame.FieldingTeam.CurrentPitcher.LastName;

            CurrentPitcher = (Player)fullGame.FieldingTeam.CurrentPitcher;
            CurrentHitter = fullGame.BattingTeam.CurrentHitter;
            AbString = fullGame.CurrentAb.ResultString;
            RunnerOnFirst = fullGame.CurrentInning.CurrentRunners.OnFirst?.Runner?.FullName;
            RunnerOnSecond = fullGame.CurrentInning.CurrentRunners.OnSecond?.Runner?.FullName;
            RunnerOnThird = fullGame.CurrentInning.CurrentRunners.OnThird?.Runner?.FullName;
            PreviousAbs = fullGame.CurrentInning.Events.OrderByDescending(o => o.Sequence).Skip(1).Select(s => s.ToString()).ToList();
            var inningStack = new Stack<InningModel>();
            foreach (var inning in fullGame.Innings.Select(s => InningModel.Create(s, s.Half == HalfInning.Top ? AwayLogo : HomeLogo)))
                inningStack.Push(inning);
            while (inningStack.Any())
                Innings.Add(inningStack.Pop());
            var homeScore = 0;
            var awayScore = 0;
            var scoreStack = new Stack<ScoringPlayModel>();
            var x = fullGame.Innings.Where(w => w.Runs > 0).GroupBy(g => g.Number);
            foreach (var fullInning in x)
            {
                var vm = ScoringPlayModel.Create(fullInning.ToList(), HomeLogo, AwayLogo, homeScore, awayScore);
                homeScore = vm.Plays.First().HomeScore;
                awayScore = vm.Plays.First().AwayScore;
                scoreStack.Push(vm);
            }
            while (scoreStack.Any())
                ScoringPlays.Add(scoreStack.Pop());
            Pitches = fullGame.CurrentAb.Pitches.Select(s => s.ToString()).ToList();
        }
        public LiveGameModel(GameData game, Repository repo) : this()
        {
            var seasonStats = repo.GetSeasonHittingStats(game.GameDate.Year, game.Playoff, game.GameDate);
            Fill(game.FullGame, game.GameDate, game.Location);
            HomeBox = HomeBoxScore.Create(game, seasonStats);
            AwayBox = AwayBoxScore.Create(game, seasonStats);
        }
        public IList<string> PreviousAbs { get; set; }
        public IList<string> Pitches { get; set; }
        public IDictionary<Position, string> Defense { get; private set; }
        public List<InningModel> Innings { get; private set; }
        public List<ScoringPlayModel> ScoringPlays { get; set; }
        public bool GameIsOver { get; set; }
        public bool IsStarted { get; set; }
        public string GameTime { get; set; }
        public int Balls { get; set; }
        public int Strikes { get; set; }
        public int Outs { get; set; }
        public string RelativePath { get; set; }
        public string Title { get; private set; }
        public int HomeScore { get; private set; }
        public int AwayScore { get; private set; }
        public int HomeHits { get; private set; }
        public int HomeErrors { get; private set; }
        public int AwayHits { get; private set; }
        public int AwayErrors { get; private set; }
        public string HomeName { get; private set; }
        public string AwayName { get; private set; }
        public string LocationText { get; private set; }
        public string CityText { get; private set; }
        public string AwayLogo { get; private set; }
        public string HomeLogo { get; private set; }
        public string PitcherLogo { get; private set; }
        public string HitterLogo { get; private set; }
        public int InningNumber { get; private set; }
        public HalfInning InningHalf { get; private set; }
        public List<int> HomeInnings { get; private set; }
        public List<int> AwayInnings { get; private set; }
        public BoxScore HomeBox { get; set; }
        public BoxScore AwayBox { get; set; }
        public Player CurrentPitcher { get; private set; }
        public Player CurrentHitter { get; private set; }
        public string AbString { get; private set; }
        public string RunnerOnFirst { get; set; }
        public string RunnerOnSecond { get; set; }
        public string RunnerOnThird { get; set; }
        public int TotalInnings { get; set; }
    }
    public class BoxScore
    {
        internal BoxScore()
        {
            HittingBox = new HittingBoxScore();
            PitchingBox = new PitchingBoxScore();
        }
        public HittingBoxScore HittingBox { get; set; }
        public PitchingBoxScore PitchingBox { get; set; }
        public int RispAb { get; set; }
        public int RispH { get; set; }
        public Player CurrentHitter { get; set; }
        internal static BoxScore Create(List<HStats> hitting, List<PStats> pitching, Team team, List<HittingStatsRow> stats = null)
        {
            var score = new BoxScore
            {
                HittingBox = new HittingBoxScore(hitting, stats),
                PitchingBox = new PitchingBoxScore(pitching),
                CurrentHitter = team.CurrentHitter,
            };
            return score;
        }
        internal static BoxScore Blank => new BoxScore();
    }
    public class HomeBoxScore : BoxScore
    {
        public HomeBoxScore(GameData game, List<HittingStatsRow> seasonStats)
        {
            if (game.HV == HV.H)
                HittingBox = new HittingBoxScore(game, seasonStats);
            else
                HittingBox = new HittingBoxScore(game.FullGame.HomeTeamHitting, new List<HittingStatsRow>());
            PitchingBox = new PitchingBoxScore(game.FullGame.HomeTeamPitching);
            RispAb = game.FullGame.HomeRisp.Abs;
            RispH = game.FullGame.HomeRisp.Hits;
        }
        internal static BoxScore Create(GameData game, List<HittingStatsRow> seasonStats)
        {
            return new HomeBoxScore(game, seasonStats);
        }
    }
    public class AwayBoxScore : BoxScore
    {
        public AwayBoxScore(GameData game, List<HittingStatsRow> seasonStats)
        {
            if (game.HV == HV.V)
                HittingBox = new HittingBoxScore(game,seasonStats);
            else
                HittingBox = new HittingBoxScore(game.FullGame.AwayTeamHitting, new List<HittingStatsRow>());
            PitchingBox = new PitchingBoxScore(game.FullGame.AwayTeamPitching);
            RispAb = game.FullGame.AwayRisp.Abs;
            RispH = game.FullGame.AwayRisp.Hits;
        }
        internal static BoxScore Create(GameData game, List<HittingStatsRow> seasonStats)
        {
            return new AwayBoxScore(game, seasonStats);
        }
    }
    public class HittingBoxScore
    {
        public HittingBoxScore()
        {
            Stats = new List<HStats>();
            SeasonStats = new List<HittingStatsRow>();
        }
        internal HittingBoxScore(GameData game, List<HittingStatsRow> seasonStats) : this()
        {
            Stats = [.. game.HittingStats.Select(HStats.Create)];

            //SeasonStats = [.. game.HittingStats.Select(s => HittingStatsRow.Sum(s.Profile.SeasonHittingTo(game.GameDate)))];
            SeasonStats = seasonStats;
        }
        public HittingBoxScore(List<HStats> hitting, List<HittingStatsRow> stats)
        {
            hitting.Cast<IHasPlayer>().SetDuplicatePlayers();
            Stats = hitting;
            SeasonStats = stats;
        }
        public StatModel Doubles => new StatModel("2B", Stats.Where(w => w.Doubles > 0).Select(s => new HittingLine(s.Player.ToString(), s.Doubles, SeasonStats?.SingleOrDefault(q => q.Player.Number == s.Player.Number)?.Doubles ?? 0)).ToList());
        public StatModel Triples => new StatModel("3B", Stats.Where(w => w.Triples > 0).Select(s => new HittingLine(s.Player.ToString(), s.Triples, SeasonStats?.SingleOrDefault(q => q.Player.Number == s.Player.Number)?.Triples ?? 0)).ToList());
        public StatModel HomeRuns => new StatModel("HR", Stats.Where(w => w.HR > 0).Select(s => new HittingLine(s.Player.ToString(), s.HR, SeasonStats?.SingleOrDefault(q => q.Player.Number == s.Player.Number)?.HomeRuns ?? 0)).ToList());
        public StatModel Rbis => new StatModel("RBI", Stats.Where(w => w.RBI > 0).Select(s => new HittingLine(s.Player.ToString(), s.RBI, SeasonStats?.SingleOrDefault(q => q.Player.Number == s.Player.Number)?.Rbis ?? 0)).ToList());
        public StatModel StolenBases => new StatModel("SB", Stats.Where(w => w.SB > 0).Select(s => new HittingLine(s.Player.ToString(), s.SB, SeasonStats?.SingleOrDefault(q => q.Player.Number == s.Player.Number)?.StolenBases ?? 0)).ToList());
        public StatModel CaughtStealing => new StatModel("CS", Stats.Where(w => w.CS > 0).Select(s => new HittingLine(s.Player.ToString(), s.CS, SeasonStats?.SingleOrDefault(q => q.Player.Number == s.Player.Number)?.CaughtStealing ?? 0)).ToList());
        public List<HStats> Stats { get; set; }
        public List<HittingStatsRow> SeasonStats { get; set; }
    }
    public class HittingLine
    {
        public HittingLine(string name, int gameTotal, int seasonTotal = 0)
        {
            PlayerName = name;
            GameTotal = gameTotal;
            SeasonTotal = seasonTotal;
        }

        public string PlayerName { get; set; }
        public int GameTotal { get; set; }
        public int SeasonTotal { get; set; }
    }
    public class PitchingBoxScore
    {
        public PitchingBoxScore()
        {
            Stats = new List<PStats>();
        }
        internal PitchingBoxScore(GameData game, List<HittingStatsRow> seasonStats) : this()
        {
            Stats = [.. game.PitchingStats.Select(PStats.Create)];
        }
        public PitchingBoxScore(List<PStats> pitching)
        {
            Stats = pitching;
        }
        public PitchingStatModel Pitches => new PitchingStatModel("Pitches-Strikes", Stats.Where(w => w.Pitches > 0).Select(s => new KeyValuePair<string, string>(s.Player.ToString(), $"{s.Pitches} - {s.Strikes}")));
        public PitchingStatModel BattersFaced => new PitchingStatModel("Batters Faced", Stats.Select(s => new KeyValuePair<string, string>(s.Player.ToString(), s.BF.ToString())));
        public PitchingStatModel BattedBalls => new PitchingStatModel("Ground Balls-Fly Balls", Stats.Select(s => new KeyValuePair<string, string>(s.Player.ToString(), $"{s.GroundOuts} - {s.FlyOuts}")));
        public List<PStats> Stats { get; set; }
    }
    public class StatModel
    {
        public StatModel() { }
        public StatModel(string v, List<HittingLine> stats)
        {
            Stat = v;
            Stats = stats;
        }
        public string Stat { get; set; }
        public List<HittingLine> Stats { get; set; }
    }
    public class PitchingStatModel
    {
        public PitchingStatModel(string stat, IEnumerable<KeyValuePair<string, string>> displayString)
        {
            Stat = stat;
            Display = displayString;
        }
        public string Stat { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Display { get; set; }
    }
}