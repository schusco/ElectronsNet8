using Electrons.Core.Net8;
using System;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class PlayerModel
    {
        public PlayerModel() { }
        private PlayerModel(RosterRow s)
        {
            Id = s.Id;
            Name = string.Join(" ", s.FirstName, string.IsNullOrEmpty(s.Nickname) ? "" : $"\"{s.Nickname}\"", s.LastName);
            Height = s.Height.ToHeightString();
            Weight = s.Weight.ToString();
            BirthPlace = s.BirthPlace;
            Position = string.Join(",", new[] { s.Pos1, s.Pos2, s.Pos3 }.Where(w => !string.IsNullOrEmpty(w)));
            RookieYear = s.RookieYear ?? DateTime.Today.Year;
            Number = s.Number == 0 ? "00" : s.Number.ToString();
        }
        public int Id { get; set; }
        [LinkColumn(NavUrl = "Profile", SortOrder = 5), LinkParameter(Parameter = "Id", Field = "Id")]
        public string Name { get; set; }
        [TableColumn(SortOrder = 10)]
        public string Height { get; set; }
        [TableColumn(SortOrder = 15)]
        public string Weight { get; set; }
        [TableColumn(HeaderText = "Birth Place", SortOrder = 20)]
        public string BirthPlace { get; set; }

        [TableColumn(HeaderText = "Position(s)", SortOrder = 25)]
        public string Position { get; set; }

        [TableColumn(HeaderText = "Rookie Year", SortOrder = 30)]
        public int RookieYear { get; set; }
        [TableColumn(HeaderText = "#", SortOrder = 2)]
        public string Number { get; set; }
        public override string ToString() => $"{Name} ({Number})";
        internal static PlayerModel Create(RosterRow arg) => new PlayerModel(arg);
    }
}