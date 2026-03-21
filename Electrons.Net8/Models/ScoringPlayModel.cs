using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class ScoringPlayModel
    {
        public ScoringPlayModel()
        {
            Plays = new List<ScoringPlay>();
        }
        public string InningText { get; set; }
        public List<ScoringPlay> Plays { get; set; }
        public string HomeLogo { get; set; }
        public string AwayLogo { get; set; }
        internal static ScoringPlayModel Create(List<Inning> innings, string homeLogo, string awayLogo, int hscore, int ascore)
        {
            var vm = new ScoringPlayModel();
            var inningNumber = innings.First().Number;
            if (inningNumber == 1)
                vm.InningText = "1ST INNING";
            else if (inningNumber == 2) vm.InningText = "2ND INNING";
            else if (inningNumber == 3) vm.InningText = "3RD INNING";
            else vm.InningText = $"{inningNumber}TH INNING";
            vm.HomeLogo = homeLogo;
            vm.AwayLogo = awayLogo;

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
        public override string ToString() => ScoreText;
    }
}