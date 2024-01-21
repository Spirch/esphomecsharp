using System.Collections.Generic;

namespace esphomecsharp.Model;

public sealed class Settings
{
    public string DBFileName { get; init; }
    public int TotalInsertInterval { get; init; }
    public int ShowErrorInterval { get; init; }
    public string DateTimeFormat { get; init; }
    public Dictionary<string, string> Pages { get; init; }
}
