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
        public Uri Uri { get; set; }
        public ConsoleColor Color { get; set; }
        public int Row { get; set; }
        public int ServerTimeOut { get; set; }
        public Stopwatch LastActivity { get; set; }
        public EState State { get; set; }

        public override string ToString()
        {
            return $"Name: {Name} FriendlyName: {FriendlyName} Row: {Row} LastActivity: {LastActivity.Elapsed.TotalSeconds}";
        }
    }
}
