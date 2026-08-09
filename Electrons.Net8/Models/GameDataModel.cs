using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using System;

namespace Electrons.Net8.Models
{
    public class GameDataModel : ILogo
    {
        public int GameId { get; set; }
        public int Hscore { get; set; }
        public int Ascore { get; set; }
        public string HvInd => !IsHome ? "@" : "vs.";
        public bool Finished => GameDate <= DateTime.Now.Actual() && (Hscore > 0 || Ascore > 0);
        public DateTime GameDate { get; set; }
        public string GameDescr => GameDate >= DateTime.Now.Actual() ? ShortGameString : Opponent;
        public virtual string GameText => $"{HvInd} {(GameDate >= DateTime.Now.Actual() ? ShortGameString : Opponent)}";
        private string ShortGameString { get; set; }
        public string Opponent { get; set; }
        public bool IsHome { get; set; }
        public virtual string AwayTeam => !IsHome ? "Electrons" : Opponent;
        public virtual string HomeTeam => IsHome ? "Electrons" : Opponent;
        public virtual string OpponentLogo => !IsHome ? Extensions.GetHomeLogo(this) : Extensions.GetAwayLogo(this);
        public virtual string GetScore() => GameData.GetScore(IsHome, Hscore, Ascore);
        public string Region { get; set; } = "";
        public string Division { get; set; }
        internal static GameDataModel Create(GameData arg)
        {
            return new GameDataModel
            {
                GameId = arg.GameId,
                GameDate = arg.GameDate,
                IsHome = arg.IsHome,
                Opponent = arg.Opponent,
                Hscore = arg.HomeRuns,
                Ascore = arg.AwayRuns,
                ShortGameString = arg.GameString,
                Region = arg.Region,
                Division = arg.Division
            };
        }
        internal static GameDataModel Create(ScoreboardApi.Models.GameScore arg)
        {
            bool isHome = arg.HomeTeamId == 1;
            var team = isHome ? arg.AwayTeam : arg.HomeTeam;
            var opponent = team.Division == "CMBA" ? team.Name : $"{team.Region} {team.Name}";
            return new GameDataModel
            {
                GameId = arg.GameId,
                GameDate = arg.GameDate,
                IsHome = arg.HomeTeamId == 1,
                Opponent = opponent,
                Hscore = arg.HomeRuns,
                Ascore = arg.AwayRuns,
                ShortGameString = $"{(team.Name)} - {arg.Location.ShortName} {arg.GameDate.ToShortTimeString()}",
                Region = team.Region,
                Division = team.Division
            };
        }

        internal static ILogo CreateFromApigame(ScoreboardApi.Models.GameScore displayGameApi)
        {
            var isHome = displayGameApi.HomeTeamId == 1;
            return new GameDataModel
            {
                IsHome = isHome,
                Opponent = isHome ? displayGameApi.AwayTeam.Name : displayGameApi.HomeTeam.Name,
                Division = isHome ? displayGameApi.AwayTeam.Division : displayGameApi.HomeTeam.Division,
                Region = isHome ? displayGameApi.AwayTeam.Region : displayGameApi.HomeTeam.Region
            };
        }
    }
}