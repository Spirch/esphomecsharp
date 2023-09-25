using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace esphomecsharp.Model
{
    public sealed class ConsoleAction
    {
        public EConsoleScreen Screen { get; set; }
        public Action PreAction { get; set; }
        public Action Action { get; set; }
        public Action PostAction { get; set; }
    }
}
