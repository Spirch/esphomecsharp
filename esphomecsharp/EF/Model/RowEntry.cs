using System.Collections.Generic;

namespace esphomecsharp.EF.Model
{
    sealed public class RowEntry : IDbItem
    {
        public int? RowEntryId { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }

        public ICollection<Event> Event { get; set; }
    }
}
