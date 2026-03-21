using System;
using System.Linq;

namespace Electrons.Core.Net8
{
    public class RosterRow
    {
        public int Number { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Height { get; set; }
        public int Weight { get; set; }
        public string BirthPlace { get; set; }
        public string Pos1 { get; set; }
        public string Pos2 { get; set; }
        public string Pos3 { get; set; }
        public int? RookieYear { get; set; }
        public string Nickname { get; set; }
        public int Id { get; set; }
        public string Email { get; set; }
        public override string ToString() => $"{LastName},{FirstName} ({Number})";
    }
    public class BirthdayModel
    {
        public DateTime BirthDate { get; set; }
        public string Name => $"{FirstName} {LastName}";
        public int Age => DateTime.Now.Year - BirthDate.Year;
        public string LastName { get; internal set; }
        public string FirstName { get; internal set; }
        public string DisplayText(int year)
        {
            var ageAt = year - BirthDate.Year;
            var pluraltext = Name.Last() == 's' ? "'" : "'s";
            return $"{Name}{pluraltext} birthday ({ageAt})";
        }
        public override string ToString() => $"{Name} ({Age})";
    }
}
