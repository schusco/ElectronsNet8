using ScoreboardApi.Models;

namespace Scorebook.ViewObjects
{
    public class GameScoreWrapper(GameScore game)
    {
        public event GameScoreUpdateEventHandler? GameScoreUpdated;
        public int GameId { get; private set; } = game.GameId;
        public Team? HomeTeam { get; private set; } = game.HomeTeam;
        public Team? AwayTeam { get; private set; } = game.AwayTeam;
        public DateTime GameDate { get; private set; } = game.GameDate;
        public DateTime? EndDateTime { get; private set; } = game.EndDateTime;
        public DateTime? StartDateTime { get; private set; } = game.StartDateTime;
        public string DisplayString => $"{AwayTeam?.Name} @ {HomeTeam?.Name}  {GameDate:g}";
        public static GameScoreWrapper Create(GameScore game) => new(game);
        internal void SetStartDateTime(DateTime? startTime)
        {
            if (startTime.HasValue && !_game.StartDateTime.HasValue)
            {
                StartDateTime = startTime.Value;
                _game.StartDateTime = StartDateTime;
                OnGameScoreUpdated();
            }
        }
        private void OnGameScoreUpdated()
        {
            GameScoreUpdated?.Invoke(this, new GameScoreEventArgs(_game));
        }

        private readonly GameScore _game = game;
    }

    public delegate Task GameScoreUpdateEventHandler(object sender, GameScoreEventArgs e);

    public class GameScoreEventArgs(GameScore game) : EventArgs
    {
        internal GameScore Game = game;
    }
}
