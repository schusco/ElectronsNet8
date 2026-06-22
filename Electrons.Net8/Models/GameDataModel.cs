using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using Microsoft.AspNetCore.Html;
using System;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class GameDataModel
    {
        public int GameId { get; set; }
        public int Hscore { get; set; }
        public int Ascore { get; set; }
        public string HvInd => !IsHome ? "@" : "vs.";
        public bool Finished => GameDate <= DateTime.Now.Actual() && (Hscore > 0 || Ascore > 0);
        public DateTime GameDate { get; set; }
        public string OpponentLogo => IsHome ? GetHomeLogo() : GetAwayLogo();
        public string GameDescr => GameDate >= DateTime.Now.Actual() ? ShortGameString : Opponent;
        public virtual string GameText => $"{HvInd} {(GameDate >= DateTime.Now.Actual() ? ShortGameString : Opponent)}";
        private string ShortGameString { get; set; }
        public string Opponent { get; set; }
        public bool IsHome { get; set; }
        public virtual string AwayTeam => !IsHome ? "Electrons" : Opponent;
        public virtual string HomeTeam => IsHome ? "Electrons" : Opponent;
        private string logoUrl => "/Content/images/logos";
        public virtual string GetAwayLogo() => GetLogo(HV.V);
        public string Eleclogo => $"~{logoUrl}/nextOuting_electrons.png";
        public virtual string GetHomeLogo() => GetLogo(HV.H);
        public virtual string GetLogo(HV hv)
        {
            if ((hv == HV.H && IsHome) || (hv == HV.V && !IsHome))
                return Eleclogo;
            return string.IsNullOrEmpty(Opponent) ? "" : $"~{logoUrl}/nextOuting_{Opponent.Replace(" ", "").ToLower()}.png";
        }
        public virtual string GetScore() => GameData.GetScore(IsHome, Hscore, Ascore);
        internal static GameDataModel Create(IGameData arg)
        {
            return new GameDataModel
            {
                GameId = arg.GameId,
                GameDate = arg.GameDate,
                IsHome = arg.IsHome,
                Opponent = arg.Opponent,
                Hscore = arg.HomeRuns,
                Ascore = arg.AwayRuns,
                ShortGameString = arg.GameString
            };
        }
    }
}