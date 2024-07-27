using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace esphomecsharp.Model;
public class AppSettingsRowInfo
{
    public string Prefix { get; set; }
    public string Suffix { get; set; }
    public int Column { get; set; }
    public int Width { get; set; }
    public string Name { get; set; }
    public string Unit { get; set; }
    public bool IsTotalDailyEnergy { get; set; }
    public bool IsTotalPower { get; set; }
    public decimal RecordDelta { get; set; }
    public int RecordThrottle { get; set; }
    public bool Hidden { get; set; }
}