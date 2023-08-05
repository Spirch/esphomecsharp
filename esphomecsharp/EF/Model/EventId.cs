using System.Collections.Generic;

namespace esphomecsharp.EF.Model
{
    sealed public class EventId : IDbItem
    {
        public int? Id { get; set; }
        public string Value { get; set; }

        public ICollection<Event> EspHomeEvent { get; set; }
    }
}
