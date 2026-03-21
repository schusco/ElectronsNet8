using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Games
{
    public class League
    {
        private League()
        {
            Teams = new List<Team>();
            Fields = new List<Field>();
        }
        public IList<Team> Teams { get; private set; }
        public IList<Field> Fields { get; private set; }
        public int GameLength { get; private set; }
        public string Name { get; private set; }
        public override string ToString() => $"{Name} - {Teams.Count} teams";
        public static League Create(string name, int gameLength) => new League { Name = name, GameLength = gameLength };
        public Team this[string name] => Teams.SingleOrDefault(s => s.Name == name);
        public static List<League> CreateFromFile(string rosterPath, out League current)
        {
            var leagues = new List<League>();
            current = null;
            try
            {
                using (var fs = File.OpenRead(rosterPath))
                {
                    var xdoc = XDocument.Load(fs);
                    foreach (var leagueEl in xdoc.Descendants().Where(w => w.Name == "League"))
                    {
                        var league = Load(leagueEl);
                        if (leagueEl.Attributes().Any(a => a.Name == "current"))
                            current = league;
                        leagues.Add(league);
                    }
                }
                if (current is null)
                    current = leagues.First();
            }
            catch (Exception)
            {
                throw;
            }
            return leagues;
        }

        private static League Load(XElement leagueEl)
        {
            var league = new League();

            var lengthEl = leagueEl.Descendants().SingleOrDefault(w => w.Name == "GameLength");
            league.GameLength = int.Parse(lengthEl?.Value ?? "7");
            league.Name = leagueEl.Attributes().SingleOrDefault(s => s.Name == "name")?.Value ?? "New League";
            var fieldEl = leagueEl.Descendants().SingleOrDefault(w => w.Name == "Fields");
            if (!(fieldEl is null))
                foreach (var el in fieldEl.Descendants().Where(w => w.Name == "Field"))
                    league.Fields.Add(Field.Load(el));
            foreach (var el in leagueEl.Descendants().Where(w => w.Name == "Team"))
            {
                var team = new Team(el.Attribute("name").Value);
                league.Teams.Add(team);
                foreach (var dec in el.Descendants().Where(w => w.Name == "Player"))
                {
                    var player = Player.Load(dec);
                    team.AddPlayer(player);
                }
                var hfEl = el.Descendants().SingleOrDefault(s => s.Name == "HomeField");
                if (!(hfEl is null))
                    team.SetHomeField(Field.Load(hfEl));
            }

            return league;
        }

        internal XElement Xml(bool current)
        {
            var leagueEl = new XElement("League");
            leagueEl.SetAttributeValue("name", Name);
            if (current)
                leagueEl.SetAttributeValue("current", true);
            leagueEl.Add(new XElement("GameLength", GameLength));
            foreach (var team in Teams)
            {
                var el = new XElement("Team");
                el.SetAttributeValue("name", team.Name);
                foreach (var player in team.Roster)
                    el.Add(player.Xml);
                leagueEl.Add(el);
                if (!(team.HomeField is null))
                    el.Add(new XElement("HomeField", team.HomeField.Xml));
            }
            var fieldEl = new XElement("Fields");
            leagueEl.Add(fieldEl);
            foreach (var field in Fields)
                fieldEl.Add(field.Xml);
            return leagueEl;
        }
        public static void SaveAll(IList<League> leagues, string rosterPath, string current)
        {
            var mainEl = new XElement("Leagues");
            foreach (var league in leagues)
                mainEl.Add(league.Xml(current == league.Name));
            var xdoc = new XDocument(mainEl);
            xdoc.Save(rosterPath);
        }
    }

    public class Field
    {
        public string Name { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string FieldText => $"{Name} {City}, {State}";
        internal XElement Xml
        {
            get
            {
                var leagueEl = new XElement("Field");
                leagueEl.Add(new XElement("Name", Name));
                leagueEl.Add(new XElement("City", City));
                leagueEl.Add(new XElement("State", State));
                return leagueEl;
            }
        }
        internal static Field Load(XElement el)
        {
            return new Field
            {
                Name = el.Descendants().Single(s => s.Name == "Name").Value,
                City = el.Descendants().Single(s => s.Name == "City").Value,
                State = el.Descendants().Single(s => s.Name == "State").Value
            };
        }
        public override string ToString() => FieldText;

        public override bool Equals(object obj)
        {
            if (!(obj is Field field))
                return false;
            return field.Name == Name;
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
