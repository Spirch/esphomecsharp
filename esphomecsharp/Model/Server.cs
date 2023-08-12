using System;
using System.Diagnostics;
using System.Threading;

namespace esphomecsharp.Model
{
    sealed public class Server
    {
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Uri { get; set; }
        public ConsoleColor Color { get; set; }
        public int Row { get; set; }
        public Stopwatch LastActivity { get; set; }
    }
}
