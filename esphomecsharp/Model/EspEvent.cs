using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace esphomecsharp.Model
{
    public class EspEvent
    {
        public string Id { get; set; }
        public object Value { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
    }
}
