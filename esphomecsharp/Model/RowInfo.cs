using System;
using System.Diagnostics;

namespace esphomecsharp.Model;

sealed public class RowInfo
{
    public Server Server { get; set; }
    public string Name { get; set; }
    public string Unit { get; set; }
    public int Padding { get; set; }
    public int Col { get; set; }
    public ConsoleColor Color { get; set; }
    public decimal RecordDelta { get; set; }
    public int RecordThrottle { get; set; }
    public string LastPrint { get; set; }
    public decimal LastValue { get; set; }
    public Stopwatch LastRecordSw { get; set; }
    public bool Hidden { get; set; }

    public int? DbDescId { get; set; }

    public override string ToString()
    {
        return $"Name: {Name} Unit: {Unit} Col: {Col} DbDescId: {DbDescId} LastValue: {LastPrint} LastRecordSw: {LastRecordSw.Elapsed.TotalSeconds} Server: {Server} RecordDelta: {RecordDelta} RecordThrottle: {RecordThrottle}";
    }
}
