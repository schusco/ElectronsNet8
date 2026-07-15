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
            var useApiData = apidata.Count > 0;
            string[] calmonths = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
            if (month < 1)
                Month = 1;
            else if (month > 12)
                Month = 12;
            else
                Month = month;
            Year = year;
            Monthtext = string.Format("{0} {1}", calmonths[month - 1], year);
            DaysInMonth = DateTime.DaysInMonth(year, month);
            DateTime firstday = new(year, month, 1);
            DaysToSkip = (int)firstday.DayOfWeek;
            HasSchedule = useApiData || repo.GetGamesByYear(DateTime.Now.Year).Any();
            var gdata = useApiData ? apidata.Select(GameDataModel.Create) : [.. repo.GetGamesByMonth(Month, Year).Select(GameDataModel.Create)];
            Dictionary<DateTime, string> evdata = repo.GetEventsByMonth(Month, Year);
            var birthdays = repo.GetBirthdays(month).GroupBy(g => g.BirthDate.Day).ToDictionary(k => k.Key, v => v.Select(s => s.DisplayText(year)));
            for (int i = 1; i <= DaysInMonth; i++)
                Days.Add(i, new ScheduleDayModel(i, gdata, evdata, birthdays, Month, Year));
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
        public ScheduleDayModel(int i, IEnumerable<GameDataModel> gdata, Dictionary<DateTime, string> eventData, Dictionary<int, IEnumerable<string>> birthdayData, int month, int year)
        {
            Year = year;
            Month = month;
            Games = gdata.Where(w => w.GameDate.Day == i).OrderBy(o => o.GameDate);
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