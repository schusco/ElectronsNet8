using Electrons.Core.Net8;
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

        public MainModel(Repository repo, GameSettings settings, IWebHostEnvironment env, List<GameScore> apiData, List<StandingsRow> standings = null)
        {

            JumboText = settings.JumboText;
            var nextOutingTime = DateTime.Now.Actual().AddHours(-3);
            var lastOuting = repo.GetNextOuting(DateTime.Now.Actual().AddHours(-18));
            var nextOuting = repo.GetNextOuting(nextOutingTime);
            if (lastOuting != nextOuting && DateTime.Now.AddHours(12) < nextOuting.GameDate)
                DisplayLastGame = true;
            var displayOuting = DisplayLastGame && lastOuting != null ? lastOuting : nextOuting;
            if (displayOuting != null)
            {
                if (displayOuting.GameDate.Date == DateTime.Now.Actual().Date && displayOuting.GameDate < DateTime.Now.Actual())
                    NextGameInProgress = true;
                if (displayOuting.GameFile != null)
                    NextGameRecap = true;

                var gd = GameDataModel.Create(displayOuting);
                HomeLogo = gd.GetHomeLogo();
                AwayLogo = gd.GetAwayLogo();
                HomeTeam = gd.HomeTeam;
                AwayTeam = gd.AwayTeam;

                GameDate = gd.GameDate.ToString("g");
                Location = displayOuting.Location.Field;
                if (NextGameInProgress)
                {
                    GameScore currentGame;
                    if (displayOuting.HV == HV.H)
                        currentGame = apiData.SingleOrDefault(s => s.GameDate.Date == DateTime.Today && s.HomeTeam.Name == "Electrons" && s.AwayTeam.Name == nextOuting.Opponent);
                    else
                        currentGame = apiData.SingleOrDefault(s => s.GameDate.Date == DateTime.Today && s.HomeTeam.Name == nextOuting.Opponent && s.AwayTeam.Name == "Electrons");
                    if (currentGame != null)
                    {
                        GameId = currentGame.GameId;
                        HomeScore = currentGame.HomeRuns.ToString() ?? "0";
                        AwayScore = currentGame.AwayRuns.ToString() ?? "0";
                        if (currentGame.Status == "Scheduled")
                            LiveInning = currentGame.GameDate.ToShortTimeString();
                        else if (currentGame.Status.Contains("Top"))
                        {
                            IsTopHalfOfInning = true;
                            LiveInning = currentGame.Status.Replace("Top of", "").Trim();
                        }
                        else if (currentGame.Status.Contains("Bottom"))
                        {
                            IsTopHalfOfInning = false;
                            LiveInning = currentGame.Status.Replace("Bottom of", "").Trim();
                        }
                        else
                            LiveInning = currentGame.Status ?? "";
                    }
                    else
                        LiveInning = nextOuting.GameDate.ToShortTimeString();
                }
                else if (DisplayLastGame)
                {
                    LiveInning = "Final";
                    var currentGame = apiData.Where(w => w.GameDate < DateTime.Now).OrderBy(o => o.GameDate).LastOrDefault();
                    var test = apiData.Where(w => w.GameDate < DateTime.Now);
                    var test2 = test.OrderBy(o => o.GameDate).LastOrDefault();
                    if (currentGame != null)
                    {
                        GameId = currentGame.GameId;
                        HomeScore = currentGame.HomeRuns.ToString();
                        AwayScore = currentGame.AwayRuns.ToString();
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
        public bool? IsTopHalfOfInning { get; set; }
    }
}