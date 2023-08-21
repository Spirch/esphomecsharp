﻿using System;
using System.Globalization;

namespace esphomecsharp.EF.Model
{
    sealed public class Event : IDbItem
    {
        public Event()
        {
                
        }

        public long EventId { get; set; }

        public int RowEntryId { get; set; }
        public long UnixTime { get; set; }
        public decimal Data
        {
            get
            {
                if(Value is decimal valDec)
                    return valDec;

                if (decimal.TryParse(Value.ToString(), NumberStyles.Number | NumberStyles.AllowExponent, null, out decimal dec))
                    return Truncate(dec, 2);

                if(bool.TryParse(Value.ToString(), out bool bo))
                    return Convert.ToDecimal(bo);

                throw new FormatException(Value.ToString());
            }
            set
            {
                Value = value;
            }
        }

        public string Id { get; set; }
        public object Value { get; set; }
        public string Name { get; set; }
        public string State { get; set; }

        public RowEntry EspHomeId { get; set; }


        decimal Truncate(decimal d, byte decimals)
        {
            decimal r = Math.Round(d, decimals);

            if (d > 0 && r > d)
            {
                return r - new decimal(1, 0, 0, false, decimals);
            }
            else if (d < 0 && r < d)
            {
                return r + new decimal(1, 0, 0, false, decimals);
            }

            return r;
        }
    }
}
