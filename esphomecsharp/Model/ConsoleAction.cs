using System;
using System.Threading.Tasks;

namespace esphomecsharp.Model;

public sealed class ConsoleAction
{
    public EConsoleScreen Screen { get; set; }
    public Func<Task> PreAction { get; set; }
    public Func<Task> Action { get; set; }
    public Func<Task> PostAction { get; set; }

    public static readonly Func<Task> NoOp = async () => { await Task.CompletedTask; };
}
