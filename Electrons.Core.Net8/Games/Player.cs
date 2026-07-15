using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Games
{
    [Component]
    public abstract class PlayerBase
    {
        public int Id { get; set; }
        public int Number => int.TryParse(DisplayNumber, out int num) ? num : 0;
        [ComponentProperty]
        public string DisplayNumber { get; set; }
        [ComponentProperty]
        public string FirstName { get; set; }
        [ComponentProperty]
        public string LastName { get; set; }
        [JsonIgnore]
        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(FirstName))
                    return $"{DisplayNumber} - {LastName}";
                if (string.IsNullOrEmpty(LastName))
                    return $"{DisplayNumber} - {FirstName}";
                else
                    return $"{DisplayNumber} - {LastName}, {(_duplicate && FirstName.Length > 1 ? FirstName[..2] : FirstName[0].ToString())}";
            }
        }
        [JsonIgnore]
        public string FullNameWithPos => $"{DisplayNumber} - {LastName} {(!string.IsNullOrEmpty(FirstName) ? $", {FirstName[0]}" : "")} - {Position?.PositionString}";
        public abstract Position Position { get; protected set; }
        [JsonIgnore]
        public Bats Bats { get; set; }
        [JsonIgnore]
        public Throws Throws { get; set; }
        [JsonIgnore]
        public bool IsPitcher => Position?.IsPitcher ?? false;
        public bool IsMemberOf(Team team) => team.Roster.Contains(this);
        [JsonIgnore]
        public bool IsUnknown { get; internal set; }
        [JsonIgnore]
        public IEnumerable<HStats> GameStats { get; internal set; }
        [JsonIgnore]
        public IEnumerable<AtBat> AtBats
        {
            get
            {
                AtBat[] ct = new AtBat[_abs?.Count ?? 0];
                _abs.CopyTo(ct);
                return ct;
            }
        }
        [JsonIgnore]
        public virtual XElement Xml
        {
            get
            {
                var xel = new XElement("Player", new XElement("Number", Number), new XElement("FirstName", FirstName), new XElement("LastName", LastName));
                if (Position != null)
                    xel.Add(new XElement("Position", Position.PositionString));
                return xel;
            }
        }
        public void AdAtBat(AtBat ab) => _abs.Add(ab);
        public override string ToString()
        {
            if (IsUnknown)
                return $"Player #{Number}";
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(FirstName) && FirstName.Length > 1)
                sb.Append($"{FirstName[0]}. ");
            sb.Append(LastName);
            return sb.ToString();
        }
        public string DisplayName
        {
            get
            {
                var sb = new StringBuilder();
                if (IsUnknown || string.IsNullOrEmpty(LastName))
                    sb.Append($"{LastName} #{DisplayNumber}");
                else
                    sb.Append(LastName);
                if (string.IsNullOrEmpty(FirstName))
                    sb.Append($" - {Number}");
                return sb.ToString();

            }
        }
        public void SetPosition(Position pos) => Position = pos;
        public void Update(string num, string lastName, string first)
        {
            if (int.TryParse(num, out int number))
            {
                DisplayNumber = num;
                LastName = lastName;
                FirstName = first;
            }
        }
        private class Seasons
        {
            public Seasons(List<HStats> list) => Stats = list;
            public IEnumerable<HStats> Stats { get; private set; }
            //  public HStats this[int year] => Stats.Single(w => w.Year == year);
        }
        protected internal void SetDuplicate() => _duplicate = true;
        protected internal bool IsDuplicate => _duplicate;
        protected List<AtBat> _abs;
        protected bool _duplicate = false;
    }
    public class Player : PlayerBase
    {
        public Player() => _abs = new List<AtBat>();
        public Player(string lastName, string number) : this()
        {
            LastName = lastName;
            FirstName = "";
            DisplayNumber = number;
        }
        public override Position Position { get; protected set; }
        public Player HittingFor { get; private set; }
        public static bool operator ==(Player lhs, Player rhs) => lhs?.Equals(rhs) ?? false;
        public static bool operator !=(Player lhs, Player rhs) => !(lhs == rhs);
        public override bool Equals(object obj)
        {
            if (!(obj is Player pl))
                return false;
            return pl.LastName == LastName && pl.Number == Number;
        }
        public override int GetHashCode() => $"{Number}{LastName}".GetHashCode();
        public static Player Unknown() => Unknown(0);
        public static Player Unknown(int number) => new Player("Player", number.ToString()) { FirstName = "Unknown", IsUnknown = true };
        public static Player PlaceHolder => new Player("--Select--", "0") { FirstName = "" };
        public static Player Blank => new Player { LastName = string.Empty, FirstName = string.Empty };
        public static Player Create(int number, string first, string last, int id = 0) => new Player(last, number.ToString()) { FirstName = first, Id = id };
        internal static Player Load(XElement xel)
        {
            var p = new Player();
            return p.UpdateFromXml(xel);
        }
        public static explicit operator Pitcher(Player player)
        {
            return new Pitcher
            {
                FirstName = player?.FirstName,
                LastName = player?.LastName,
                DisplayNumber = player?.DisplayNumber,
                IsUnknown = player.IsUnknown
            };
        }
        protected internal Player UpdateFromXml(XElement xel)
        {
            LastName = !string.IsNullOrWhiteSpace(xel.Element("LastName").Value) ? xel.Element("LastName").Value : "Player";
            FirstName = !string.IsNullOrWhiteSpace(xel.Element("FirstName").Value) ? xel.Element("FirstName").Value : "Unknown";
            DisplayNumber = xel.Element("Number").Value;
            Position = Position.FromString(xel.Element("Position")?.Value);
            if (xel.Descendants().Any(a => a.Name == "HittingFor"))
                HittingFor = Load(xel.Descendants("HittingFor").Descendants("Player").Single());

            IsUnknown = string.IsNullOrEmpty(LastName) || FirstName == "Unknown" && LastName == "Player";
            return this;
        }
        public override XElement Xml
        {
            get
            {
                var xel = base.Xml;
                if (!(HittingFor is null))
                    xel.Add(new XElement("HittingFor", HittingFor?.Xml));
                return xel;
            }
        }
        public void SetDhFor(Player dhPlayer)
        {
            HittingFor = dhPlayer;
        }
    }
    public class Pitcher : PlayerBase
    {
        internal Pitcher() { }
        public override Position Position { get => Position.P; protected set { } }
        [JsonIgnore]
        public bool EnteredInSaveSituation { get; private set; }
        [JsonIgnore]
        public bool HeldLead { get; private set; } = true;
        [JsonIgnore]
        public bool EarnedSave { get; private set; }
        [JsonIgnore]
        public bool IsPitcherOfRecord { get; private set; }
        internal void SetPitcherOfRecord() => IsPitcherOfRecord = true;
        internal void LetPitcherOffHook() => IsPitcherOfRecord = false;
        internal void IsSaveSituation() => EnteredInSaveSituation = true;
        internal void BlewLead() => HeldLead = false;
        internal void AwardSave() => EarnedSave = true;
        public static bool operator ==(Pitcher lhs, Pitcher rhs) => lhs?.Equals(rhs) ?? false;
        public static bool operator !=(Pitcher lhs, Pitcher rhs) => !(lhs == rhs);
        public override bool Equals(object obj)
        {
            if (!(obj is Pitcher pl))
                return false;
            return pl.LastName == LastName && pl.Number == Number;
        }
        public override int GetHashCode() => $"{Number}{LastName}".GetHashCode();
        public static explicit operator Player(Pitcher pitcher)
        {
            if (pitcher is null)
                return null;
            var player = new Player(pitcher?.LastName ?? "Unknown", pitcher?.DisplayNumber ?? "0")
            {
                FirstName = pitcher?.FirstName ?? "Player"
            };
            player.IsUnknown = pitcher.IsUnknown;
            return player;
        }
    }
    public class Position : IEqualityComparer<Position>
    {
        private Position(int posNumber, Positions pos, string positionText, string posString, string logPosString)
        {
            PositionNumber = posNumber;
            _positions |= pos;
            PositionString = posString ?? pos.ToString();
            PositionText = positionText;
            LongPositionString = logPosString;
        }
        public bool IsPitcher => _positions.HasFlag(Positions.P);
        public int PositionNumber { get; private set; }
        public string PositionString { get; private set; }
        public string LongPositionString { get; private set; }
        public string PositionText { get; private set; }
        public override bool Equals(object obj)
        {
            if (!(obj is Position pos))
                return false;
            return PositionNumber == pos.PositionNumber;
        }
        public override int GetHashCode() => PositionNumber;
        private readonly Positions _positions;
        [Flags]
        private enum Positions
        {
            // [Description("0")]
            DH = 0,
            [Description("1")]
            P = 1,
            [Description("2")]
            C = 2,
            [Description("3")]
            _1B = 4,
            [Description("4")]
            _2B = 8,
            [Description("5")]
            _3B = 16,
            [Description("6")]
            SS = 32,
            IF = 56,
            [Description("7")]
            LF = 64,
            [Description("8")]
            CF = 128,
            [Description("9")]
            RF = 256,
            // [Description("10")]
            EH = -1,
            OF = 448
        }
        public string DisplayString
        {
            get
            {
                var positionList = new List<Positions>();
                foreach (Positions val in Enum.GetValues(typeof(Positions)))
                {
                    if (_positions.HasFlag(val))
                    {
                        if (_positions == 0)
                            return "DH";
                        if (_positions > 0 && (val == Positions.DH || val == Positions.EH))
                            continue;
                        if (val.IsIn(Positions._2B, Positions._3B, Positions.SS) && _positions.HasFlag(Positions.IF))
                            continue;
                        if (val.IsIn(Positions.CF, Positions.LF, Positions.RF) && _positions.HasFlag(Positions.OF))
                            continue;
                        positionList.Add(val);
                    }
                }
                return positionList.Aggregate(string.Empty, (current, next) => $"{current} / {next}").Trim().TrimStart('/').Trim();
            }
        }
        public override string ToString() => $"{LongPositionString} ({PositionNumber})";
        public static Position P => new Position(1, Positions.P, "pitcher", "P", "Pitcher");
        public static Position C => new Position(2, Positions.C, "catcher", "C", "Catcher");
        public static Position _1B => new Position(3, Positions._1B, "first", "1B", "First Base");
        public static Position _2B => new Position(4, Positions._2B, "second", "2B", "Second Base");
        public static Position _3B => new Position(5, Positions._3B, "third", "3B", "Third Base");
        public static Position SS => new Position(6, Positions.SS, "shortstop", "SS", "Shortstop");
        public static Position LF => new Position(7, Positions.LF, "left", "LF", "Left Field");
        public static Position CF => new Position(8, Positions.CF, "center", "CF", "Center Field");
        public static Position RF => new Position(9, Positions.RF, "right", "RF", "Right Field");
        public static Position EH => new Position(10, Positions.EH, "", "EH", "Extra Hitter");
        public static Position DH => new Position(0, Positions.DH, "", "DH", "Designated Hitter");
        public static IEnumerable<Position> All
        {
            get
            {
                yield return DH;
                yield return P;
                yield return C;
                yield return _1B;
                yield return _2B;
                yield return _3B;
                yield return SS;
                yield return LF;
                yield return CF;
                yield return RF;
                yield return EH;
            }
        }
        public static explicit operator Position(int pos) => All.SingleOrDefault(s => s.PositionNumber == pos);
        public static explicit operator int(Position pos) => pos.PositionNumber;
        public static Position FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return EH;
            foreach (var pos in All)
            {
                if (pos.PositionString == value || pos.PositionNumber.ToString() == value)
                    return pos;
            }
            throw new BaseballGameException("Invalid Position");
        }
        public static bool operator ==(Position lhs, Position rhs)
        {
            if (lhs is null && rhs is null)
                return true;
            if (lhs is null)
                return false;
            return lhs.Equals(rhs);
        }
        public static bool operator !=(Position lhs, Position rhs)
        {
            return !(lhs == rhs);
        }
        public bool Equals(Position x, Position y)
        {
            return x?.PositionNumber == y?.PositionNumber;
        }

        public int GetHashCode(Position obj)
        {
            return obj.PositionNumber.GetHashCode();
        }
    }
}
