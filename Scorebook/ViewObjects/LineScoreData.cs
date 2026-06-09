using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;

namespace Scorebook.ViewObjects
{
    public class LineScoreData
    {
        public LineScoreData(int number)
        {
            InningNumber = number.ToString();
        }
        public LineScoreData(int number, IGrouping<int, Inning> grp)
        {
            InningNumber = number.ToString();
            var top = grp.SingleOrDefault(s => s.Half == HalfInning.Top);
            AwayRuns = top?.Runs.ToString() ?? "X";
            var btm = grp.SingleOrDefault(s => s.Half == HalfInning.Bottom);
            HomeRuns = btm?.Runs.ToString() ?? "X";
        }

        public string InningNumber { get; set; } = "-";
        public string HomeRuns { get; set; } = "-";
        public string AwayRuns { get; set; } = "-";
        public bool IsTotalsColumn { get; set; }
    }
}
