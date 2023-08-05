using System;

namespace esphomecsharp.EF.Model
{
    sealed public class Event : IDbItem
    {
        //composite key, order matter
        public int DescId { get; set; }
        public DateTimeOffset Date { get; set; }
        public long PkSuffix { get; set; }
        //composite key, order matter

        public double? ValueDouble
        {
            get
            {
                if (double.TryParse(Value.ToString(), out double value))
                    return value;

                return null;
            }
            set
            {
                Value = value;
            }
        }

        public string ValueString
        {
            get
            {
                return Value.ToString();
            }
            set
            {
                Value = value;
            }
        }

        public string Id { get; set; }
        public dynamic Value { get; set; }
        public string Name { get; set; }
        public string State { get; set; }

        public EventId EspHomeId { get; set; }
    }
}
