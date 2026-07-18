using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using ScoreboardApi.Models;

using ApiTeam = ScoreboardApi.Models.Team;
namespace Scorebook.ViewObjects
{
    public class GameScoreWrapper(GameScore game)
    {
        public event GameScoreUpdateEventHandler? GameScoreUpdated;

        public int GameId { get; private set; } = game.GameId;
        public ApiTeam? HomeTeam { get; private set; } = game.HomeTeam;
        public int HomeTeamId => game.HomeTeamId;
        public ApiTeam? AwayTeam { get; private set; } = game.AwayTeam;
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
                if (!game.StartDateTime.HasValue)
                {
                    _game.StartDateTime = StartDateTime;
                    OnGameScoreUpdated();
                }
            }
        }
        internal void OnGameScoreUpdated()
        {
            GameScoreUpdated?.Invoke(this, new GameScoreEventArgs(_game));
        }
        internal string GameStatus => _game?.Status ?? "";
        internal void SetInningStatus(string status)
        {
            if (_game.Status != "Final")
                _game.Status = status;
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

    }

    public delegate Task GameScoreUpdateEventHandler(object sender, GameScoreEventArgs e);

    public class GameScoreEventArgs(GameScore game) : EventArgs
    {
        internal GameScore Game = game;
    }

}
