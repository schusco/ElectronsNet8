using Electrons.Core.Net8.Games;
using Electrons.Core.Net8.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class PlayByPlayModel
    {
        public PlayByPlayModel(BaseballGame game, Repository repo, int gameId)
        {
            Innings = game.Innings.Select(s => InningModel.Create(s, "")).ToList();
            var gd = repo.GetGameById(gameId);
            Title = $"{game.AwayTeam.Name} @ {game.HomeTeam.Name} {gd.GameDate:M/d/yyyy h:mm tt} @ {gd.Location.Field}";
        }
        public IList<InningModel> Innings { get; set; }
        public string Title { get; set; }
    }
    public class InningModel
    {
        public InningModel()
        {
            Events = new List<InningEventModel>();
        }
        public string Logo { get; set; }
        public string Description { get; set; }
        public IList<InningEventModel> Events { get; set; }
        public string Summary { get; set; }
        internal static InningModel Create(Inning inning, string logo)
        {
            var model = new InningModel
            {
                Description = inning.ToString(),
                Summary = inning.InningSummary,
                Logo = logo
            };
            var inningStack = new Stack<InningEventModel>();
            inning.Events.ToList().ForEach(f => inningStack.Push(InningEventModel.Create(f)));
            while (inningStack.Any())
                model.Events.Add(inningStack.Pop());
            return model;
        }
    }
    public class InningEventModel
    {
        public string EventText { get; set; }
        public bool ScoringPlay { get; set; }
        internal static InningEventModel Create(AtBat arg)
        {
            return new InningEventModel
            {
                EventText = arg.ToString(),
                ScoringPlay = arg.AdvancingRunners.Any(a => a.Runs > 0)
            };
        }
        public override string ToString() => EventText;
    }
}