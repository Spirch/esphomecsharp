using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace esphomecsharp.EF.Model
{
    public sealed class Error : IDbItem
    {
        public int? Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public string DeviceName { get; set; }
        public string Message { get; set; }
        public bool IsHandled { get; set; }
    }
}
