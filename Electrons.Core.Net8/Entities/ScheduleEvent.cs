using NHibernate.Mapping.Attributes;
using System;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "events")]
    public class ScheduleEvent
    {
        protected ScheduleEvent() { }
        protected ScheduleEvent(DateTime evDate, string evText)
        {
            Date = evDate;
            Event = evText;
        }
        [Id(Name = "Id", Column = "ID"), Generator(Class = "native")]
        public virtual int Id { get; protected set; }
        [Property]
        public virtual DateTime Date { get; protected set; }
        [Property]
        public virtual string Event { get; protected set; }
        public override string ToString() => Event;

        public static ScheduleEvent CreateNew(DateTime evDate, string evText) => new ScheduleEvent(evDate, evText);

        public virtual void SetEvent(string evString) => Event = evString;
    }
}
