using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using static Electrons.Net8.Models.ScoringPlayModel;

namespace Electrons.Net8.Models
{
    public class ScoringPlayModel
    {
        public ScoringPlayModel(string homeLogo, string awayLogo)
        {
            Plays = new List<ScoringPlay>();
            HomeLogo = homeLogo;
            AwayLogo = awayLogo;
        }
        public string InningText { get; set; }
        public List<ScoringPlay> Plays { get; set; }
        public string HomeLogo { get; set; }
        public string AwayLogo { get; set; }
        internal static ScoringPlayModel Create(List<Inning> innings, string homeLogo, string awayLogo, int hscore, int ascore)
        {
            var vm = new ScoringPlayModel(homeLogo, awayLogo);
            SetInningText(vm, innings.First().Number);

            var playStack = new Stack<ScoringPlay>();
            foreach (var inning in innings)
            {
                var logo = inning.Half == HalfInning.Top ? awayLogo : homeLogo;
                foreach (var play in inning.Events.Where(w => w.AllRunnerEvents.Any(a => a.Runs > 0)))
                {
                    if (inning.Half == HalfInning.Top)
                        ascore += play.Runs;
                    else
                        hscore += play.Runs;
                    playStack.Push(ScoringPlay.Create(play, logo, hscore, ascore));
                }
            }
            while (playStack.Any())
                vm.Plays.Add(playStack.Pop());
            return vm;
        }
        internal static ScoringPlayModel Create(List<ScoreboardApi.Models.Inning> innings, string homeLogo, string awayLogo, int hscore, int ascore)
        {
            var vm = new ScoringPlayModel(homeLogo, awayLogo);
            SetInningText(vm, innings.First().Number);
            var playStack = new Stack<ScoringPlay>();
            foreach (var inning in innings)
            {
                var logo = inning.IsTopHalf ? awayLogo : homeLogo;
                foreach (var play in inning.Atbats.Where(w => w.IsScoringPlay))
                {
                    if (inning.IsTopHalf)
                        ascore += play.Scoring;
                    else
                        hscore += play.Scoring;
                    playStack.Push(ScoringPlay.Create(play, logo, hscore, ascore));
                }
            }
            while (playStack.Any())
                vm.Plays.Add(playStack.Pop());
            return vm;
        }

        internal static List<ScoringPlayModel> CreateScoringPlays(List<ScoreboardApi.Models.Inning> innings, string homeLogo, string awayLogo)
        {
            var list = new List<ScoringPlayModel>();
            int homeScore = 0;
            int awayScore = 0;
            var scoreStack = new Stack<ScoringPlayModel>();
            var x = innings.Where(w => w.Runs > 0).GroupBy(g => g.Number);
            foreach (var fullInning in x)
            {
                var vm = Create(fullInning.ToList(), homeLogo, awayLogo, homeScore, awayScore);
                if (vm.Plays.Any())
                {
                    homeScore = vm.Plays.First().HomeScore;
                    awayScore = vm.Plays.First().AwayScore;
                    scoreStack.Push(vm);
                }
            }
            while (scoreStack.Any())
                list.Add(scoreStack.Pop());
            return list;
        }

        private static void SetInningText(ScoringPlayModel vm, int inningNumber)
        {
            if (inningNumber == 1)
                vm.InningText = "1ST INNING";
            else if (inningNumber == 2) vm.InningText = "2ND INNING";
            else if (inningNumber == 3) vm.InningText = "3RD INNING";
            else vm.InningText = $"{inningNumber}TH INNING";
        }

        public class ScoringPlay
        {
            public string ScoreText { get; set; }
            public string TeamLogo { get; set; }
            public int HomeScore { get; set; }
            public int AwayScore { get; set; }

            internal static ScoringPlay Create(AtBat ab, string logo, int hscore, int ascore)
            {
                return new ScoringPlay
                {
                    ScoreText = ab.ScoreText,
                    TeamLogo = logo,
                    HomeScore = hscore,
                    AwayScore = ascore
                };
            }
            internal static ScoringPlay Create(ScoreboardApi.Models.Atbat ab, string logo, int hscore, int ascore)
            {
                return new ScoringPlay
                {
                    ScoreText = ab.Result,
                    TeamLogo = logo,
                    HomeScore = hscore,
                    AwayScore = ascore
                };
            }
            public override string ToString() => ScoreText;
        }
    }
}