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
        public string HvInd => HV == HV.V ? "@" : "vs.";
        public bool Finished => GameDate <= DateTime.Now && (Hscore > 0 || Ascore > 0);
        public DateTime GameDate { get; set; }
        public string OpponentLogo => HV == HV.V ? GetHomeLogo() : GetAwayLogo();
        public string GameDescr => GameDate >= DateTime.Now ? ShortGameString : Opponent;            
        public virtual string GameText => $"{HvInd} {(GameDate >= DateTime.Now ? ShortGameString : Opponent)}";
        private string ShortGameString { get; set; }
        public string Opponent { get; set; }
        public HV HV { get; set; }
        public virtual string AwayTeam => HV == HV.V ? "Electrons" : Opponent;
        public virtual string HomeTeam => HV == HV.H ? "Electrons" : Opponent;
        private string logoUrl => "/Content/images/logos";
        public virtual string GetAwayLogo() => GetLogo(HV.V);
        public string Eleclogo => $"~{logoUrl}/nextOuting_electrons.png";
        public virtual string GetHomeLogo() => GetLogo(HV.H);
        public virtual string GetLogo(HV hv)
        {
            if (HV == hv)
                return Eleclogo;
            return string.IsNullOrEmpty(Opponent) ? "" : $"~{logoUrl}/nextOuting_{Opponent.Replace(" ", "").ToLower()}.png";
        }
        public virtual string GetScore() => GameData.GetScore(HV, Hscore, Ascore);
        internal static GameDataModel Create(GameData arg)
        {
            return new GameDataModel
            {
                GameId = arg.GameId,
                GameDate = arg.GameDate,
                HV = arg.HV,
                Opponent = arg.Opponent,
                Hscore = arg.Innings.Sum(s => s.HomeRuns ?? 0),
                Ascore = arg.Innings.Sum(s => s.AwayRuns ?? 0),
                ShortGameString = $"{arg.Opponent} - {arg.Location.ShortFieldName} {arg.GameDate.ToShortTimeString()} {(arg.Wood ? "(WB)" : "")}"
            };
        }
    }
}