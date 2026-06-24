using Electrons.Core.Net8;
using Electrons.Core.Net8.Infrastructure;
using ScoreboardApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class MainModel
    {
        public MainModel() { }
        internal const string DateFormatString = "M/d/yyyy h:m tt";
        public MainModel(Repository repo, GameSettings settings, List<GameScore> apiData, List<StandingsRow> standings = null)
        {
            JumboText = settings.JumboText;
            var nextOutingData = repo.GetNextOuting(apiData);
            DisplayLastGame = nextOutingData.DisplayLastGame;
            DbGameId = nextOutingData.DisplayGameDb?.GameId;
            GameId = nextOutingData.DisplayGameApi?.GameId;
            if (nextOutingData.DisplayGameApi != null)
            {
                if (nextOutingData.DisplayGameApi.GameDate.Date == DateTime.Now.Actual().Date && nextOutingData.DisplayGameApi.GameDate < DateTime.Now.Actual())
                    NextGameInProgress = true;
                if (nextOutingData.DisplayGameDb?.GameFile != null)
                    NextGameRecap = true;
                if (nextOutingData?.DisplayGameApi != null)
                {
                    HomeLogo = nextOutingData.DisplayGameApi.HomeTeam.Name.GetLogo();
                    AwayLogo = nextOutingData.DisplayGameApi.AwayTeam.Name.GetLogo();
                    HomeTeam = nextOutingData.DisplayGameApi.HomeTeam.Name;
                    AwayTeam = nextOutingData.DisplayGameApi.AwayTeam.Name;
                    GameDate = nextOutingData.DisplayGameApi.GameDate.ToString(DateFormatString);
                    Location = nextOutingData.DisplayGameApi.Location?.FieldName;
                }
                else
                {
                    var gd = GameDataModel.Create(nextOutingData.DisplayGameDb);
                    HomeLogo = gd.GetHomeLogo();
                    AwayLogo = gd.GetAwayLogo();
                    HomeTeam = gd.HomeTeam;
                    AwayTeam = gd.AwayTeam;
                    GameDate = gd.GameDate.ToString(DateFormatString);
                    Location = nextOutingData.DisplayGameDb.Location.Field;
                }

                if (NextGameInProgress)
                {
                    if (nextOutingData.DisplayGameApi != null)
                    {                        
                        HomeScore = nextOutingData.DisplayGameApi.HomeRuns.ToString() ?? "0";
                        AwayScore = nextOutingData.DisplayGameApi.AwayRuns.ToString() ?? "0";
                        if (nextOutingData.DisplayGameApi.Status == "Scheduled")
                            LiveInning = nextOutingData.DisplayGameApi.GameDate.ToShortTimeString();
                        else if (nextOutingData.DisplayGameApi.Status.Contains("Top"))
                        {
                            IsTopHalfOfInning = true;
                            LiveInning = nextOutingData.DisplayGameApi.Status.Replace("Top of", "").Trim();
                        }
                        else if (nextOutingData.DisplayGameApi.Status.Contains("Bottom"))
                        {
                            IsTopHalfOfInning = false;
                            LiveInning = nextOutingData.DisplayGameApi.Status.Replace("Bottom of", "").Trim();
                        }
                        else
                            LiveInning = nextOutingData.DisplayGameApi.Status ?? "";
                    }
                    else
                        LiveInning = nextOutingData.DisplayGameApi.GameDate.ToShortTimeString();
                }
                else if (nextOutingData.DisplayLastGame)
                {
                    LiveInning = nextOutingData.DisplayGameApi.Status;
                    NextOutingText = "Last Game";
                    if (nextOutingData.DisplayGameApi != null)
                    {                        
                        HomeScore = nextOutingData.DisplayGameApi.HomeRuns.ToString();
                        AwayScore = nextOutingData.DisplayGameApi.AwayRuns.ToString();
                    }
                }
            }
            if (standings == null)
                Standings = repo.GetStandings("CMBA").Select(s => StandingsModel.Create(s.Team, s.Wins, s.Losses, s.Ties, s.Points))
                    .OrderByDescending(o => o.Points).ThenByDescending(o => o.Wins);
            else
                Standings = standings.Select(s => StandingsModel.Create(s.Name, s.Wins, s.Losses, s.Ties, s.Points))
                    .OrderByDescending(o => o.Points).ThenByDescending(o => o.Wins);
        }
        public string NextOutingText { get; set; } = "Next Outing";
        public string JumboText { get; set; }
        public string HomeTeam { get; set; }
        public string HomeLogo { get; set; }
        public string AwayTeam { get; set; }
        public string AwayLogo { get; set; }
        public string LiveInning { get; set; } = "";
        public string HomeScore { get; set; } = "0";
        public string AwayScore { get; set; } = "0";
        public int? GameId { get; set; }
        public int? DbGameId { get; set; }
        public string GameDate { get; set; }
        public string Location { get; set; }
        public bool NextGameInProgress { get; set; }
        public bool NextGameRecap { get; set; }
        public bool DisplayLastGame { get; set; }
        public IEnumerable<StandingsModel> Standings { get; set; }
        public bool? IsTopHalfOfInning { get; set; }
    }
}