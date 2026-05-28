using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using ScoreboardApi.Models;
using ApiAb = ScoreboardApi.Models.Atbat;
using ApiInning = ScoreboardApi.Models.Inning;
using ApiTeam = ScoreboardApi.Models.Team;
namespace Scorebook.ViewObjects
{
    public class GameScoreWrapper(GameScore game)
    {
        public event GameScoreUpdateEventHandler? GameScoreUpdated;
        public event InningUpdateEventHandler? InningUpdated;
        public event AbUpdateEventHandler? AtbatUpdated;
        public int GameId { get; private set; } = game.GameId;
        public ApiTeam? HomeTeam { get; private set; } = game.HomeTeam;
        public int HomeTeamId => game.HomeTeamId;
        public ApiTeam? AwayTeam { get; private set; } = game.AwayTeam;
        public DateTime GameDate { get; private set; } = game.GameDate;
        public DateTime? EndDateTime { get; private set; } = game.EndDateTime;
        public DateTime? StartDateTime { get; private set; } = game.StartDateTime;
        public string DisplayString => $"{AwayTeam?.Name} @ {HomeTeam?.Name}  {GameDate:g}";
        public ApiInning? Inning => _currentInning;
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
        private void OnGameScoreUpdated()
        {
            GameScoreUpdated?.Invoke(this, new GameScoreEventArgs(_game));
        }
        private void OnInningUpdated()
        {
            if (_currentInning is not null)
                InningUpdated?.Invoke(this, new InningEventArgs(_currentInning));
        }
        private void OnAtBatUpdated()
        {
            if (_currentAtbat is null) return;
            if (_currentInning is null) return;
            if (_currentInning.Id > 0)
            {
                _currentAtbat.InningId = _currentInning.Id;
                AtbatUpdated?.Invoke(this, new AbEventArgs(_currentAtbat));
            }
        }
        private void SetInningStatus()
        {
            _game.Status = $"{_halfInning} of {_inningNumber}";
        }
        internal void SetNextInning(AtBat currentAb)
        {
            if (_inningNumber == 0)
            {
                _halfInning = HalfInning.Top;
                _inningNumber = 1;
            }
            else if (_halfInning == HalfInning.Top)
                _halfInning = HalfInning.Bottom;
            else
            {
                _halfInning = HalfInning.Top;
                _inningNumber++;
            }
            SetInningStatus();
            OnGameScoreUpdated();
            _currentInning = new ApiInning
            {
                GameId = GameId,
                Errors = 0,
                Hits = 0,
                Number = _inningNumber,
                Runs = 0,
                IsTopHalf = _halfInning == HalfInning.Top
            };
            _currentInning.Atbats.Add(ConvertAb(currentAb));
            OnInningUpdated();
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
        internal void UpdateCurrentInning(object? sender, InningEventArgs e)
        {
            _currentInning = e.Inning;
            if (e.Inning.Atbats.Count == 1)
            {
                _currentAtbat = e.Inning.Atbats.First();
                CheckPlayers(_currentAtbat);
            }
        }
        internal void UpdateCurrentAtbat(object? sender, AbEventArgs e)
        {
            _currentAtbat = e.Ab;
            CheckPlayers(e.Ab);
        }

        private void CheckPlayers(ApiAb ab)
        {
            if (ab.BatterId > 0 && ab.Batter != null)
            {
                var roster = HomeTeamId == ab.Batter.TeamId ? Services.ApiService.ApiRosters[HomeTeam.Name]
                    : Services.ApiService.ApiRosters[AwayTeam.Name];
                if (!roster.Any(a => a.Id == ab.BatterId))
                    roster.Add(ab.Batter);
            }
            if (ab.PitcherId > 0 && ab.Pitcher != null)
            {
                var roster = HomeTeamId == ab.Pitcher?.TeamId ? Services.ApiService.ApiRosters[HomeTeam.Name]
                    : Services.ApiService.ApiRosters[AwayTeam.Name];
                if (!roster.Any(a => a.Id == ab.PitcherId))
                    roster.Add(ab.Pitcher);
            }
        }

        internal void UpdateInning(int runs, int hits, int errors)
        {
            if (_currentInning == null)
                return;
            if (_currentInning.Runs == runs && _currentInning.Hits == hits && _currentInning.Errors == errors && _currentInning.Id != 0)
                return;
            _currentInning.Runs = runs;
            _currentInning.Hits = hits;
            _currentInning.Errors = errors;
            OnInningUpdated();
        }
        private ApiAb ConvertAb(AtBat currentAb)
        {
            var hittingTeam = _currentInning.IsTopHalf ? AwayTeam : HomeTeam;
            var pitchingTeam = _currentInning.IsTopHalf ? HomeTeam : AwayTeam;
            var batter = Services.ApiService.ApiRosters[hittingTeam.Name].FirstOrDefault(p => p.Number == currentAb.Batter.Number);
            var pitcher = Services.ApiService.ApiRosters[pitchingTeam.Name].FirstOrDefault(p => p.Number == currentAb.Pitcher.Number);
            
            var batterId=batter?.Id ?? 0;
            var pitcherId = pitcher?.Id ?? 0;
            var ab = new ApiAb
            {
                Sequence = currentAb.Sequence,
                BatterId = batterId,
                PitcherId = pitcherId,
                Batter = batterId > 0 ? null : new CmbaPlayer { FirstName = currentAb.Batter.FirstName, LastName = currentAb.Batter.LastName,
                    Number = currentAb.Batter.Number, TeamId = hittingTeam.Id },
                Pitcher = pitcherId > 0 ? null : new CmbaPlayer { FirstName = currentAb.Pitcher.FirstName, LastName = currentAb.Pitcher.LastName,
                    Number = currentAb.Pitcher.Number, TeamId = pitchingTeam.Id },
                Result = currentAb.ToString()
            };
            return ab;
        }
        internal void UpdateAb(ApiAb ab)
        {
            _currentAtbat = ab;
            OnAtBatUpdated();
        }
        internal void UpdateAb(AtBat ab)
        {
            if (_currentAtbat == null)
                _currentAtbat = ConvertAb(ab);
            else
            {
                if (_currentAtbat.Sequence == ab.Sequence)
                    _currentAtbat.Result = ab.ToString();
                else
                    _currentAtbat = ConvertAb(ab);
            }
            OnAtBatUpdated();
        }

        private readonly GameScore _game = game;
        private HalfInning _halfInning;
        private int _inningNumber;
        private ApiInning? _currentInning;
        private ApiAb? _currentAtbat;
    }

    public delegate Task GameScoreUpdateEventHandler(object sender, GameScoreEventArgs e);
    public delegate Task InningUpdateEventHandler(object sender, InningEventArgs e);
    public delegate Task AbUpdateEventHandler(object sender, AbEventArgs e);
    public class GameScoreEventArgs(GameScore game) : EventArgs
    {
        internal GameScore Game = game;
    }
    public class InningEventArgs(ApiInning inning) : EventArgs
    {
        internal ApiInning Inning = inning;
    }
    public class AbEventArgs(ApiAb ab) : EventArgs
    {
        internal ApiAb Ab = ab;
    }
}
