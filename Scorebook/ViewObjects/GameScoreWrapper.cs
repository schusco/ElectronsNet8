using ScoreboardApi.Models;

namespace Scorebook.ViewObjects
{
    public class GameScoreWrapper
    {
        public GameScoreWrapper(GameScore game)
        {
            HomeTeam = game.HomeTeam;
            AwayTeam = game.AwayTeam;
            GameDate = game.GameDate;
        }

        public Team? HomeTeam { get; private set; }
        public Team? AwayTeam { get; private set; }
        public DateTime GameDate { get; private set; }
        public string DisplayString => $"{AwayTeam?.Name} @ {HomeTeam?.Name}  {GameDate:g}";

        public static GameScoreWrapper Create(GameScore game) => new(game);
    }
}
