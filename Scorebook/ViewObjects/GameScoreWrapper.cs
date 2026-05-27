using Electrons.Core.Net8;
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
                _inningNumber = 1;
                _halfInning = HalfInning.Top;
                SetInningStatus();
                StartDateTime = startTime.Value;
                if (!game.StartDateTime.HasValue)
                {
                    _game.StartDateTime = StartDateTime;
                    OnGameScoreUpdated();
                }
            }
        }
        private void OnGameScoreUpdated()
        {
            GameScoreUpdated?.Invoke(this, new GameScoreEventArgs(_game));
        }
        private void SetInningStatus()
        {
            _game.Status = $"{_halfInning} of {_inningNumber}";
        }
        internal void SetNextInning()
        {
            if (_inningNumber == 0)
                return;
            if (_halfInning == HalfInning.Top)
            {
                _halfInning = HalfInning.Bottom;
            }
            else
            {
                _halfInning = HalfInning.Top;
                _inningNumber++;
            }
            SetInningStatus();
            OnGameScoreUpdated();
        }
        internal void SetScore(int homeScore, int awayScore)
        {
            _game.HomeRuns = homeScore;
            _game.AwayRuns = awayScore;
            OnGameScoreUpdated();
        }
        internal void SetGameEnded(DateTime? endTime)
        {
            _game.Status = "Final";
            if (!_game.EndDateTime.HasValue && endTime.HasValue)
                _game.EndDateTime = endTime;
            OnGameScoreUpdated();
        }

        private readonly GameScore _game = game;
        private HalfInning _halfInning;
        private int _inningNumber;
    }

    public delegate Task GameScoreUpdateEventHandler(object sender, GameScoreEventArgs e);

    public class GameScoreEventArgs(GameScore game) : EventArgs
    {
        internal GameScore Game = game;
    }
}
