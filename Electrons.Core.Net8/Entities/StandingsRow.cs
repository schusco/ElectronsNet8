using NHibernate.Mapping.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "teams")]
    public class StandingsRow
    {

        public virtual string LogoName => $@"{Logo}<span style=""display:inline-block;width:100px;padding-left:10px;text-align:left"">{Team}</span>";
        public virtual string Logo => $@"<img src=""Content/images/logos/nextOuting_{Team.Replace(" ", "")}.png"" height=""25"" width=""25"" />";
        [Id(Column = "Team", Name = "Team"), Generator(Class = "assigned"), TableColumn(HeaderText = "Team", SortOrder = 0, ColumnCss = "display: inline-block; width: 100px; padding-left: 10px; text-align: left;", ImageFormat = "~/Content/images/logos/nextOuting_{0}.png")]
        public virtual string Team { get; protected set; }
        [TableColumn(HeaderText = "W", SortOrder = 5), Property(Column = "W")]
        public virtual int Wins { get; protected set; }
        [TableColumn(HeaderText = "L", SortOrder = 10), Property(Column = "L")]
        public virtual int Losses { get; protected set; }
        [TableColumn(HeaderText = "T", SortOrder = 15), Property(Column = "T")]
        public virtual int Ties { get; protected set; }
        [TableColumn(HeaderText = "Pts.", SortOrder = 20), Property(Formula = "2*W+T-F")]
        public virtual int Points { get; protected set; }
        [Property(Column = "F")]
        public virtual int ForfeitPoints { get; protected set; }

        [Property]
        public virtual string Division { get; protected set; }
        [Property(Column = "Active")]
        public virtual bool IsActive { get; protected set; }

        public override string ToString()
        {
            return $"{Team} {Wins} - {Losses}";
        }

        public virtual void Update(int w, int l, int t, int f)
        {
            Wins = w;
            Losses = l;
            Ties = t;
            ForfeitPoints = f;
        }
    }
}
