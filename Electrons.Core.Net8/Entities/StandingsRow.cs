using NHibernate.Mapping.Attributes;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "teams")]
    public class StandingsRow
    {
        [Id(Column = "Team", Name = "Team"), Generator(Class = "assigned")]
        public virtual string Team { get; protected set; }
        [Property(Column = "W")]
        public virtual int Wins { get; protected set; }
        [Property(Column = "L")]
        public virtual int Losses { get; protected set; }
        [Property(Column = "T")]
        public virtual int Ties { get; protected set; }
        [Property(Formula = "2*W+T-F")]
        public virtual int Points { get; protected set; }
        [Property(Column = "F")]
        public virtual int ForfeitPoints { get; protected set; }
        [Property]
        public virtual string Division { get; protected set; }
        [Property(Column = "Active")]
        public virtual bool IsActive { get; protected set; }
        [Property]
        public virtual string? Region { get; protected set; }
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
