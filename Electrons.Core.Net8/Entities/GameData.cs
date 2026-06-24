using Electrons.Core.Net8.Games;
using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Electrons.Core.Net8.Entities
{
    public interface IGameData
    {
        int GameId { get; }
        DateTime GameDate { get; }
        string Opponent { get; }
        int HomeRuns { get; }
        int AwayRuns { get; }
        bool IsHome { get; }
        string GameString { get; }
        string Division { get; }
        string Region { get; }

    }
    [Class(Table = "gameschedule")]
    public class GameData
    {
        protected GameData()
        {
            HittingStats = new List<HittingStats>();
            PitchingStats = new List<PitchingStats>();
            Innings = new List<GameInning>();
        }
        protected GameData(PlayerProfile manager) : this()
        {
            Manager = manager;
            LocationString = "";
        }
        [Id(Column = "Game_ID", Name = "GameId"), Generator(Class = "native")]
        public virtual int GameId { get; protected set; }
        [Property(Column = "HV")]
        protected string HVvalue { get; set; }
        public virtual HV HV { get { return (HV)Enum.Parse(typeof(HV), HVvalue); } }
        [Property(Column = "Game_Date")]
        public virtual DateTime GameDate { get; protected set; }
        [Property]
        public virtual string Opponent { get; protected set; }
        [ManyToOne(Column = "LocationId", ClassType = typeof(Location))]
        public virtual Location Location { get; protected set; }
        [Property(Column = "Location")]
        protected virtual string LocationString { get; set; }
        [Property(Column = "GameFile")]
        public virtual string GameFile { get; protected set; }
        public virtual BaseballGame FullGame
        {
            get
            {
                if (GameFile is null)
                    return null;
                return BaseballGame.Load(XDocument.Parse(GameFile));
            }
        }
        public virtual string GameJson() => GameFile is null ? "" : JsonSerializer.Serialize(FullGame);
        [Set(0, Name = "HittingStats", Inverse = true, Cascade = "all-delete-orphan"), Key(1, Column = "Game_Id"), OneToMany(2, ClassType = typeof(HittingStats))]
        public virtual ICollection<HittingStats> HittingStats { get; protected set; }
        [Set(0, Name = "PitchingStats", Inverse = true, Cascade = "all-delete-orphan"), Key(1, Column = "Game_Id"), OneToMany(2, ClassType = typeof(PitchingStats))]
        public virtual ICollection<PitchingStats> PitchingStats { get; protected set; }
        [Set(0, Name = "Innings", Inverse = true, Cascade = "all-delete-orphan"), Key(1, Column = "GameId"), OneToMany(2, ClassType = typeof(GameInning))]
        public virtual ICollection<GameInning> Innings { get; protected set; }

        public virtual void SetGameFile(XDocument xdoc)
        {
            GameFile = xdoc.ToString();
        }
        public virtual void AddHittingStats(HittingStatsRow statsRow, PlayerProfile player)
        {
            var stats = HittingStats.SingleOrDefault(w => w.Profile.Id == player.Id);
            if (stats == null)
            {
                stats = Entities.HittingStats.CreateNew(this, player);
                HittingStats.Add(stats);
            }
            stats.Update(statsRow);
        }
        public virtual void AddPitchingStats(PitchingStatsRow statsRow, PlayerProfile player, string dec)
        {
            var stats = PitchingStats.SingleOrDefault(w => w.Player.Id == player.Id);
            if (stats == null)
            {
                stats = Entities.PitchingStats.CreateNew(this, player, dec);
                PitchingStats.Add(stats);
            }
            stats.Update(statsRow, dec);
        }

        [Property]
        public virtual bool Playoff { get; protected set; }
        [Property]
        public virtual bool Wood { get; protected set; }
        [Property]
        public virtual string Notes { get; protected set; }
        [Property]
        public virtual bool Finals { get; protected set; }
        [ManyToOne(Column = "Manager", ClassType = typeof(PlayerProfile))]
        public virtual PlayerProfile Manager { get; protected set; }

        [ManyToOne(Column = "SP", ClassType = typeof(PlayerProfile))]
        public virtual PlayerProfile StartingPitcher { get; protected set; }

        public virtual int HomeRuns => Innings.Sum(s => s.HomeRuns ?? 0);

        public virtual int AwayRuns => Innings.Sum(s => s.AwayRuns ?? 0);

        public virtual bool IsHome => HV == HV.H;
        [Property(Formula ="(select t.Division from teams t where t.Team=Opponent)")]
        public virtual string Division { get; set; }
        [Property(Formula ="(select t2.Region from teams t2 where t2.Team=Opponent)")]
        public virtual string Region { get; set; }

        public virtual string GameString => $"{Opponent} - {Location.ShortFieldName} {GameDate.ToShortTimeString()} {(Wood ? "(WB)" : "")}";        

        public virtual LineScoreModel GetLineScore(HV hv)
        {
            var model = new LineScoreModel();
            var home = hv == HV.H;
            if (home)
            {
                model.Hits = Innings.Sum(s => s.HomeHits).ToString();
                model.Errors = Innings.Sum(s => s.HomeErrors).ToString();
                model.Runs = Innings.Sum(s => s.HomeRuns).ToString();
                model.Team = HV == HV.H ? "Electrons" : Opponent;
            }
            else
            {
                model.Hits = Innings.Sum(s => s.AwayHits).ToString();
                model.Errors = Innings.Sum(s => s.AwayErrors).ToString();
                model.Runs = Innings.Sum(s => s.AwayRuns).ToString();
                model.Team = HV == HV.V ? "Electrons" : Opponent;
            }
            foreach (var inning in Innings.OrderBy(o => o.Inning))
                model.Innings.Add(home ? inning.HomeRuns?.ToString() ?? "x" : inning.AwayRuns?.ToString() ?? "x");
            return model;
        }

        public override string ToString() => ToGameDateString();
        public virtual string ToGameDateString()
        {
            var hvInd = HV == HV.V ? "@" : "vs.";
            return $"{hvInd} {Opponent} {GameDate:g} ({Location.Field}, {Location.CityAndState})";
        }
        public virtual void Update(DateTime gameDate, HV hv, Location loc, string opponent, PlayerProfile sp, string notes = "", bool playoff = false, bool wood = false)
        {
            Update(gameDate, hv, loc);
            Notes = notes;
            Playoff = playoff;
            Wood = wood;
            Opponent = opponent;
            StartingPitcher = sp;
        }
        public static GameData CreateNew(DateTime gameDate, HV hv, Location location, PlayerProfile manager)
        {
            var gd = new GameData(manager);
            gd.Update(gameDate, hv, location);
            return gd;
        }
        public static string GetScore(bool isHome, int hscore, int ascore)
        {
            if (ascore == 0 && hscore == 0)
                return "";
            string wlInd = string.Empty;
            if (hscore == ascore)
                wlInd = "T";
            if (!isHome)
            {
                if (hscore > ascore)
                    wlInd = "L";
                else if (hscore < ascore)
                    wlInd = "W";
            }
            else if (isHome)
            {
                if (hscore < ascore)
                    wlInd = "L";
                else if (hscore > ascore)
                    wlInd = "W";
            }

            if (hscore > ascore)
                return string.Format("{2} {0}-{1}", hscore, ascore, wlInd);
            else
                return string.Format("{2} {0}-{1}", ascore, hscore, wlInd);
        }
        private void Update(DateTime gameDate, HV hv, Location location)
        {
            GameDate = gameDate;
            HVvalue = hv.ToString();
            Location = location;
            LocationString = location.Field;
        }
        public virtual void UpdateInning(int inning, int? topR, int? botR, int? topH, int? botH, int? topE, int? botE)
        {
            var updateInning = Innings.SingleOrDefault(s => s.Inning == inning);
            if (updateInning != null)
                updateInning.UpdateRuns(topR, botR, topH, botH, topE, botE);
            else
            {
                Innings.Add(GameInning.CreateNew(this, inning, topR, botR, topH, botH, topE, botE));
            }
        }

        public virtual void Postpone(DateTime? postponeTo = null)
        {
            if (postponeTo.HasValue)
                GameDate = postponeTo.Value;
            else
                GameDate = GameDate.AddYears(100);
        }
    }
}
