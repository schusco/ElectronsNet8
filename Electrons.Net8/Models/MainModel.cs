using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using ScoreboardApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class MainModel
    {
        public MainModel() { }

        public MainModel(Repository repo, GameSettings settings, IWebHostEnvironment env, GameScore apiData, List<ScoreboardApi.Models.StandingsRow> standings = null)
        {
            JumboText = settings.JumboText;
            var nextOutingTime = DateTime.Now.AddHours(-3);
            var lastOuting = repo.GetNextOuting(DateTime.Now.AddHours(-12));
            var nextOuting = repo.GetNextOuting(nextOutingTime);
            if (nextOuting != null)
            {
                if (nextOuting.GameDate.Date == DateTime.Today && nextOutingTime < DateTime.Now)
                    NextGameInProgress = true;
                if (nextOuting.GameFile != null)
                    NextGameRecap = true;
                if (lastOuting != nextOuting)
                    DisplayLastGame = true;
                var displayOuting = DisplayLastGame && lastOuting != null ? lastOuting : nextOuting;
                var gd = GameDataModel.Create(displayOuting);
                HomeLogo = gd.GetHomeLogo();
                AwayLogo = gd.GetAwayLogo();
                HomeTeam = gd.HomeTeam;
                AwayTeam = gd.AwayTeam;
                GameId = apiData?.GameId;
                GameDate = gd.GameDate.ToString("g");
                Location = nextOuting.Location.Field;
                if (NextGameInProgress)
                {
                    if (apiData != null)
                    {
                        HomeScore = apiData?.HomeRuns.ToString() ?? "0";
                        AwayScore = apiData?.AwayRuns.ToString() ?? "0";
                        LiveInning = apiData?.Status == "Scheduled" ? apiData.GameDate.ToShortTimeString() : apiData.Status ?? "";
                    }
                    else
                        LiveInning = nextOuting.GameDate.ToShortTimeString();
                }
                else if (DisplayLastGame)
                {
                    LiveInning = "Final";
                    if (apiData != null)
                    {
                        HomeScore = apiData?.HomeRuns.ToString() ?? "0";
                        AwayScore = apiData?.AwayRuns.ToString() ?? "0";

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
        public string JumboText { get; set; }
        public string HomeTeam { get; set; }
        public string HomeLogo { get; set; }
        public string AwayTeam { get; set; }
        public string AwayLogo { get; set; }
        public string LiveInning { get; set; } = "";
        public string HomeScore { get; set; } = "0";
        public string AwayScore { get; set; } = "0";
        public int? GameId { get; set; }
        public string GameDate { get; set; }
        public string Location { get; set; }
        public bool NextGameInProgress { get; set; }
        public bool NextGameRecap { get; set; }
        public bool DisplayLastGame { get; set; }
        public IEnumerable<StandingsModel> Standings { get; set; }
    }
}