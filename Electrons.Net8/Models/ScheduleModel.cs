using Electrons.Core.Net8;
using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class ScheduleModel
    {
        public ScheduleModel()
        {
            Days = new Dictionary<int, ScheduleDayModel>();
        }
        public ScheduleModel(Repository repo, int month, int year, List<ScoreboardApi.Models.GameScore> apidata)
            : this()
        {            
            Month = Math.Max(1, Math.Min(month, 12));
            Year = year;
            DateTime firstday = new(year, month, 1);
            Monthtext = firstday.ToString("MMMM yyyy");
            DaysInMonth = DateTime.DaysInMonth(year, month);            
            DaysToSkip = (int)firstday.DayOfWeek;
            HasSchedule = apidata.Count > 0 || repo.GetGamesByYear(DateTime.Now.Year).Any();
            var gdata = repo.GetGamesByMonth(Month, Year).Select(GameDataModel.Create);
            Dictionary<DateTime, string> evdata = repo.GetEventsByMonth(Month, Year);
            var birthdays = repo.GetBirthdays(month).GroupBy(g => g.BirthDate.Day).ToDictionary(k => k.Key, v => v.Select(s => s.DisplayText(year)));
            for (int i = 1; i <= DaysInMonth; i++)
                Days.Add(i, new ScheduleDayModel(i, gdata, apidata, evdata, birthdays, Month, Year));
            Links = repo.GetFieldLinks();
        }
        public int Month { get; set; }
        public int Year { get; set; }
        public int DaysToSkip { get; set; }
        public int DaysInMonth { get; set; }
        public IDictionary<int, ScheduleDayModel> Days { get; set; }
        public IDictionary<string, string> Links { get; set; }
        public string Monthtext { get; set; }
        public bool HasSchedule { get; set; }
        public int NextYear
        {
            get
            {
                var date = new DateTime(Year, Month, 1).AddMonths(1);
                return date.Year;
            }
        }
        public int NextMonth
        {
            get
            {
                var date = new DateTime(Year, Month, 1).AddMonths(1);
                return date.Month;
            }
        }
        public int PrevMonth
        {
            get
            {
                var date = new DateTime(Year, Month, 1).AddMonths(-1);
                return date.Month;
            }
        }
        public int PrevYear
        {
            get
            {
                var date = new DateTime(Year, Month, 1).AddMonths(-1);
                return date.Year;
            }
        }
    }

    public class ScheduleDayModel
    {
        public ScheduleDayModel(int i, IEnumerable<GameDataModel> gdata, List<ScoreboardApi.Models.GameScore> apidata, Dictionary<DateTime, string> eventData, Dictionary<int, IEnumerable<string>> birthdayData, int month, int year)
        {
            var useApiData = apidata.Count > 0;
            var apiDict = apidata.GroupBy(g => g.GameDate.Day).ToDictionary(k => k.Key, v => v.ToList());
            var currentGameData = gdata.Where(w => w.GameDate.Day == i).ToList();
            if (useApiData)
            {
                foreach (var game in currentGameData)
                {
                    var index = 0;
                    if (!apiDict.ContainsKey(i))
                        continue;
                    var apiGames = apiDict[i];
                    try
                    {
                        var apiGame = apiGames[index];
                        game.Ascore = apiGame.AwayRuns;
                        game.Hscore = apiGame.HomeRuns;                        
                    }
                    catch { }                    
                }
            }
            Year = year;
            Month = month;
            Games = currentGameData.OrderBy(o => o.GameDate);
            Events = eventData.Where(w => w.Key.Day == i).Select(s => s.Value);
            Birthdays = birthdayData.TryGetValue(i, out IEnumerable<string> value) ? value : [];
            DayOfMonth = i;
            if (Games.Any(a => a.IsHome))
                CellCss = "homeGame";
            else if (Games.Any(a => !a.IsHome))
                CellCss = "awayGame";
            if (Events.Any())
                CellCss = "eventCss";

            if (DayOfMonth == DateTime.Now.Actual().Day && Month == DateTime.Now.Actual().Month && Year == DateTime.Now.Actual().Year)
                CurrentDayCss = "currentDay";
        }
        public int DayOfMonth { get; set; }
        public IEnumerable<GameDataModel> Games { get; set; }
        public IEnumerable<string> Events { get; set; }
        public IEnumerable<string> Birthdays { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string CellCss { get; set; }
        public string CurrentDayCss { get; set; } = "regular";
    }
}