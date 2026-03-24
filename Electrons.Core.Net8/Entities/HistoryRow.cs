using NHibernate.Mapping.Attributes;
using System;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "history"), Discriminator(Column = "Category")]
    public class HistoryRow
    {
        [Id(Name = "Id", Column = "Id"), Generator(Class = "native")]
        public virtual int Id { get; protected set; }
        [Property]
        public virtual string YearStart { get; protected set; }
        [TableColumn(HeaderProperty = "DataHeader", SortOrder = 5), Property]
        public virtual string Data { get; protected set; }
        [TableColumn(Optional = true, SortOrder = 7), Property]
        public virtual string Finish { get; protected set; }
        public virtual string DataHeader { get; protected set; }
        [Property]
        public virtual string YearEnd { get; protected set; }
        [TableColumn(HeaderProperty = "YearHeader", SortOrder = 10)]
        public virtual string YearData
        {
            get
            {
                var endYear = YearEnd ?? "Present";
                if (YearStart == endYear)
                    return YearStart;
                return $"{YearStart} - {endYear}";
            }
        }
        public virtual string YearHeader => "Years";
        public virtual string Category { get; protected set; }

        public override string ToString() => $"{YearStart} - {Data} - {Finish}";
    }
    [Subclass(ExtendsType = typeof(HistoryRow), DiscriminatorValue = "Stadium")]
    public class StadiumHistory : DateBasedHistoryRow
    {
        public StadiumHistory() => DataHeader = "Stadium";
    }
    [Subclass(ExtendsType = typeof(HistoryRow), DiscriminatorValue = "Dues")]
    public class DuesHistory : HistoryRow
    {
        public DuesHistory() => DataHeader = "Dues";
    }
    public abstract class DateBasedHistoryRow : HistoryRow
    {
        [TableColumn(SortOrder = 15)]
        public virtual string Seasons
        {
            get
            {
                if (!int.TryParse(YearStart, out int startYear))
                    startYear = 2003;
                if (!int.TryParse(YearEnd, out int endYear))
                    endYear = DateTime.Now.Year;
                var startDate = new DateTime(startYear, 1, 1);
                var endDate = new DateTime(endYear, 1, 2);
                var span = endDate - startDate;
                var totalYears = Math.Ceiling(span.TotalDays / 365);
                return $"{totalYears} Season{(totalYears > 1 ? "s" : "")}";
            }
        }
    }
    [Subclass(ExtendsType = typeof(HistoryRow), DiscriminatorValue = "Franchise")]
    public class FranchiseHistory : DateBasedHistoryRow
    {
        public FranchiseHistory()
        {
            DataHeader = "Team";
        }
    }
    [Subclass(ExtendsType = typeof(HistoryRow), DiscriminatorValue = "Result")]
    public class ResultsHistory : HistoryRow
    {
        public ResultsHistory()
        {
            DataHeader = "Record";
        }
        [TableColumn(HeaderProperty = "YearHeader", SortOrder = 3)]
        public override string YearData => YearStart;
        public override string YearHeader => "Year";

        public static ResultsHistory Create(object[] args)
        {
            return new ResultsHistory
            {
                Data = args[0] as string,
                YearStart = args[2].ToString(),
                Finish = args[1] as string,
            };
        }

    }
    [Subclass(ExtendsType = typeof(HistoryRow), DiscriminatorValue = "Manage")]
    public class ManagerHistory : HistoryRow
    {
        public ManagerHistory()
        {
            DataHeader = "Manager";
        }

        [TableColumn(SortOrder = 20)]
        public virtual string Record
        {
            get
            {
                if (!string.IsNullOrEmpty(_recordString))
                    return _recordString;
                if (RecordBytes == null || RecordBytes.Length == 0)
                    return string.Empty;
                _recordString = System.Text.Encoding.UTF8.GetString(RecordBytes);
                return _recordString;
            }
            set
            {
                _recordString = value;
            }
        }
        [TableColumn("Playoffs", SortOrder = 25)]
        public virtual string PlayoffRecord { get; set; }
        public virtual byte[] RecordBytes { get; set; }

        [TableColumn(HeaderProperty = "YearHeader", SortOrder = 10)]
        public override string YearData => YearStart;
        public override string YearHeader => "Years";
        public static ManagerHistory Create(object[] args)
        {
            return new ManagerHistory
            {
                Data = args[0] as string,
                YearStart = args[1] as string,
                _recordString = args[3] as string,
                RecordBytes = args[3] as byte[],
            };
        }
        private string _recordString;
    }
    [Subclass(ExtendsType = typeof(HistoryRow), DiscriminatorValue = "Retired")]
    public class RetiredNumbers : HistoryRow
    {
        public RetiredNumbers()
        {
            DataHeader = "Player";
        }
        [TableColumn(HeaderText = "Number", SortOrder = 5)]

        public override string Finish { get; protected set; }
    }
    [Subclass(ExtendsType = typeof(HistoryRow), DiscriminatorValue = "Championship")]
    public class PlayoffHistory : HistoryRow
    {
        public PlayoffHistory()
        {
            DataHeader = "Record";
        }
        [TableColumn(HeaderProperty = "YearHeader", SortOrder = 2)]
        public override string YearData => YearStart;
        public override string YearHeader { get { return "Year"; } }
        [TableColumn(HeaderText = "Result", SortOrder = 10)]
        public override string Finish { get; protected set; }

        public static PlayoffHistory Create(object[] args)
        {
            return new PlayoffHistory
            {
                Data = args[1] as string,
                YearStart = args[0].ToString(),
                Finish = args[2] as string,
            };
        }
    }
    [Subclass(ExtendsType = typeof(HistoryRow), DiscriminatorValue = "Player")]
    public class PlayerHistory:HistoryRow
    {
        public PlayerHistory() { }
        [TableColumn(HeaderText = "Player", SortOrder = 5)]
        public override string Data { get ; protected set ; }
        [TableColumn(HeaderText = "Date", SortOrder = 10)]
        public override string Finish { get; protected set; }
        public override string YearData => base.YearData;
        public virtual DateTime Date => DateTime.Parse(Finish);
    }
}
