using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;

namespace Electrons.Net8.Api.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string LastName { get; set; } = "";
        public bool Current { get; set; }
        public Bats Bats { get; set; }
        public Throws Throws { get; set; }
        public string POS1 { get; set; }="";
        public string? POS2 { get; set; }
        public string? POS3 { get; set; }
        public string? NickName { get; set; }
        public string HomeTown { get; set; } = "";
        public int Divorces { get; set; }
        public DateTime DOB { get; set; }
        public int Height { get; set; }
        public int Weight { get; set; }
        public short Image { get; set; }
        public int Uniform { get; set; }
        public string? Email { get; set; }
    }
}
