using Electrons.Core.Net8;

namespace Electrons.Net8.Models
{
    public class StandingsModel
    {
        public virtual string LogoName => $@"{Logo}<span style=""display:inline-block;width:100px;padding-left:10px;text-align:left"">{Team}</span>";

        public virtual string Logo => Team.GetLogo();
        [TableColumn(HeaderText = "Team", SortOrder = 0, ColumnCss = "display: inline-block; width: 100px; padding-left: 10px; text-align: left;", ImageFormat = "~/Content/images/logos/nextOuting_{0}.png")]
        public string Team { get; set; }
        [TableColumn(HeaderText = "W", SortOrder = 5)]
        public virtual int Wins { get; protected set; }
        [TableColumn(HeaderText = "L", SortOrder = 10)]
        public virtual int Losses { get; protected set; }
        [TableColumn(HeaderText = "T", SortOrder = 15)]
        public virtual int Ties { get; protected set; }
        [TableColumn(HeaderText = "Pts.", SortOrder = 20)]
        public virtual int Points { get; protected set; }

        public static StandingsModel Create(string team, int wins, int losses, int ties, int points)
        {
            return new StandingsModel
            {
                Team = team,
                Wins = wins,
                Losses = losses,
                Ties = ties,
                Points = points
            };
        }
    }
}
