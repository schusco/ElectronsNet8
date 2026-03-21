using NHibernate.Mapping.Attributes;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "locations")]
    public class Location
    {
        protected Location() { }
        [Id(Name = "Id"), Generator(Class = "native")]
        public virtual int Id { get; protected set; }
        [Property(Column = "FieldName")]
        public virtual string Field { get; protected set; }
        [Property(Column = "ShortName")]
        public virtual string ShortFieldName { get; protected set; }
        [Property]
        public virtual string Link { get; protected set; }
        [Property]
        public virtual bool Current { get; protected set; }
        [Property]
        public virtual string CityAndState { get; protected set; }
        [Property]
        public virtual string GoogleName { get; protected set; }
        public override string ToString() => $"{Field}  {CityAndState}";
    }
}
