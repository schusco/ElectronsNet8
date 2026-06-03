using System.Collections.Generic;

namespace Electrons.Net8.Models
{
    public class GameInningUpdateDto
    {
        public int GameId { get; internal set; }
        public List<InningModel> PlayByPlay { get; internal set; }
        public BoxScore HomeBoxScore { get; internal set; }
        public BoxScore AwayBoxScore { get; internal set; }
        public List<ScoringPlayModel> ScoringPlays { get; internal set; }
        public string HomeTeamName { get; internal set; }
        public string AwayTeamName { get; internal set; }
    }
}
