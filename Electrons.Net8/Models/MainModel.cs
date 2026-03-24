using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class MainModel
    {
        public MainModel() { }

        public MainModel(Repository repo, GameSettings settings, IWebHostEnvironment env)
        {
            JumboText = settings.JumboText;
            var data = repo.GetNextOuting(DateTime.Now);

            if (data != null)
            {                
                var gd = GameDataModel.Create(data);
                HomeLogo = gd.GetHomeLogo();
                AwayLogo = gd.GetAwayLogo();
                HomeTeam = gd.HomeTeam;
                AwayTeam = gd.AwayTeam;
                GameDate = gd.GameDate.ToString("g");
                Location = data.Location.Field;
                //var inProgress = repo.IsLiveGameInProgress;
                if (gd.GameDate.Date == DateTime.Today && gd.GameDate.AddHours(4) > DateTime.Now)
                    NextGameInProgress = true;
                else if (gd.GameDate.AddHours(18) < DateTime.Now)
                    NextGameRecap = true;
            }
            Standings = repo.GetStandings("CMBA").OrderByDescending(o => o.Points).ThenByDescending(o => o.Wins);
        }
        public string JumboText { get; set; }
        public string HomeTeam { get; set; }
        public string HomeLogo { get; set; }
        public string AwayTeam { get; set; }
        public string AwayLogo { get; set; }
        public string GameDate { get; set; }
        public string Location { get; set; }
        public bool NextGameInProgress { get; set; }
        public bool NextGameRecap { get; set; }
        public IEnumerable<StandingsRow> Standings { get; set; }
    }
}