using Electrons.Core.Net8.Games;
using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "players")]
    public class PlayerProfile : IPerson
    {
        public PlayerProfile()
        {
            Awards = new List<Award>();
            HittingStats = new List<HittingStats>();
            PitchingStats = new List<PitchingStats>();
        }
        [Id(Column = "Player_Id", Name = "Id"), Generator(Class = "native")]
        public virtual int Id { get; protected set; }
        [Property(Column = "First_Name")]
        public virtual string FirstName { get; protected set; }
        [Property(Column = "Last_Name")]
        public virtual string LastName { get; protected set; }
        public virtual string FullName => string.Format("{0} {1}", FirstName, LastName);
        public virtual string ImageFile => string.Format("/Content/images/players/{0}{1}_Magnified.png", LastName.Replace(" ", "").Trim(), FirstName.Trim());
        [Property]
        public virtual bool Current { get; protected set; }
        [Property(Column = "Bats")]
        protected virtual string BatsLR { get; set; }
        public virtual Bats Bats => (Bats)Enum.Parse(typeof(Bats), BatsLR);
        [Property(Column = "Throws")]
        protected virtual string ThrowsLR { get; set; }
        public virtual Throws Throws => (Throws)Enum.Parse(typeof(Throws), ThrowsLR);
        public virtual string Positions => string.Empty.Combine(',', POS1, POS2, POS3);
        [Property]
        public virtual string POS1 { get; protected set; }
        [Property]
        public virtual string POS2 { get; protected set; }
        [Property]
        public virtual string POS3 { get; protected set; }
        [Property]
        public virtual string Nickname { get; protected set; }
        [Property]
        public virtual string Hometown { get; protected set; }
        [Property]
        public virtual int? Divorces { get; protected set; }
        [Property]
        public virtual DateTime? DOB { get; protected set; }
        [Property]
        public virtual int? Height { get; protected set; }
        public virtual string HeightString => Height?.ToHeightString();
        [Property]
        public virtual int? Weight { get; protected set; }
        [Property(Column = "uniform")]
        public virtual int UniformNumber { get; protected set; }
        [Property(Column = "email")]
        public virtual string Email { get; protected set; }
        public virtual int Years { get; protected set; }
        public virtual int Bitches { get; protected set; }
        public virtual Player Player => Player.Create(UniformNumber, FirstName, LastName);
        [Property(Formula = "case when Nickname='XX' then 1 else 0 end")]
        public virtual bool IsHidden { get; protected set; }
        [Set(0, Name = "Awards", Inverse = true, Cascade = "all-delete-orphan"), Key(1, Column = "Player_Id"), OneToMany(2, ClassType = typeof(Award))]
        public virtual ICollection<Award> Awards { get; protected set; }
        [Set(0, Name = "HittingStats", Inverse = true, Cascade = "all-delete-orphan"), Key(1, Column = "Player_Id"), OneToMany(2, ClassType = typeof(HittingStats))]
        public virtual ICollection<HittingStats> HittingStats { get; protected set; }
        [Set(0, Name = "PitchingStats", Inverse = true, Cascade = "all-delete-orphan"), Key(1, Column = "Player_Id"), OneToMany(2, ClassType = typeof(PitchingStats))]
        public virtual ICollection<PitchingStats> PitchingStats { get; protected set; }

        public virtual IEnumerable<HittingStats> SeasonHittingTo(DateTime date) => HittingStats.Where(w => w.Game.GameDate < date && w.Game.GameDate > new DateTime(date.Year, 1, 1));
        public virtual int RookieYear { get; protected set; }
        public virtual void SetCurrent(bool isCurrent)
        {
            Current = isCurrent;
        }
        public virtual void SetBatsThrows(string bats, string throws)
        {
            BatsLR = bats;
            ThrowsLR = throws;
        }
        public virtual void SetPositions(string pos1, string pos2, string pos3)
        {
            POS1 = pos1;
            POS2 = pos2;
            POS3 = pos3;
        }
        public virtual void Update(string nickname, string hometown, int divorces, string dob, int height, int weight, string email)
        {
            Nickname = nickname;
            Hometown = hometown;
            Divorces = divorces;
            Height = height;
            Weight = weight;
            Email = email;
            DOB = string.IsNullOrEmpty(dob) ? (DateTime?)null : DateTime.Parse(dob);
        }
        public override string ToString()
        {
            return FullName;
        }
        public static PlayerProfile CreateNew(int uniform, string firstName, string lastName)
        {
            var player = new PlayerProfile
            {
                FirstName = firstName,
                LastName = lastName,
                UniformNumber = uniform
            };
            return player;
        }
        public virtual void UpdateName(string fname, string lname)
        {
            FirstName = fname;
            LastName = lname;
        }
    }

    public interface IPerson
    {
        string FirstName { get; }
        string LastName { get; }
    }
}
