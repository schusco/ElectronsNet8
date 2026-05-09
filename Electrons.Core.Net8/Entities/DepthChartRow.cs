using NHibernate.Mapping.Attributes;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "dc")]
    public class DepthChartRow
    {
        [Id(Name = "Id", Column = "Id"), Generator(Class = "assigned")]
        public virtual int Id { get; protected set; }
        [Property]
        public virtual DcPosition Position { get; protected set; }
        [Property(Column = "`Rank`")]
        public virtual int Rank { get; protected set; }
        [ManyToOne(Column = "Player", ClassType = typeof(PlayerProfile))]
        public virtual PlayerProfile Player { get; protected set; }
        public virtual void Update(PlayerProfile playerProfile)
        {
            Player = playerProfile;
        }
        public static DepthChartRow Create(int position, int rank, PlayerProfile player)
        {
            return new DepthChartRow
            {
                Position = (DcPosition)position,
                Rank = rank,
                Player = player
            };
        }        
    }
}