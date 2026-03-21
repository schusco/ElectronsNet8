using NHibernate.Mapping.Attributes;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "awards")]
    public class Award
    {
        [Id(Name = "Id"), Generator(Class = "native")]
        public virtual int Id { get; protected set; }
        [ManyToOne(Column = "Player_Id", Name = "Player", ClassType = typeof(PlayerProfile))]
        public virtual PlayerProfile Player { get; protected set; }
        [Property(Column = "Award")]
        public virtual string AwardData { get; protected set; }

        public override string ToString() => AwardData;
    }
}
