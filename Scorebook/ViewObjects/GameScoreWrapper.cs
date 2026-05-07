using ScoreboardApi.Models;

namespace Scorebook.ViewObjects
{
    public class GameScoreWrapper(GameScore game)
    {
        public Team? HomeTeam { get; private set; } = game.HomeTeam;
        public Team? AwayTeam { get; private set; } = game.AwayTeam;
        public DateTime GameDate { get; private set; } = game.GameDate;
        public DateTime? EndDateTime { get; private set; } = game.EndDateTime;
        public DateTime? StartDateTime { get; private set; } = game.StartDateTime;
        public string DisplayString => $"{AwayTeam?.Name} @ {HomeTeam?.Name}  {GameDate:g}";
        public static GameScoreWrapper Create(GameScore game) => new(game);
    }
}
