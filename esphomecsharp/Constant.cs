using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace esphomecsharp;
public static class Constant
{
    public const string RES_TOTAL_DAILY_ENERGY = "Total Daily Energy:";
    public const string RES_TOTAL_POWER = "Total Power:";
    public const string RES_TOTAL = "_total";
    public const string RES_NAME = "_name";
    public const string RES_KILLO_WATT = "kW";
    public const string RES_WATT = "W";

    public const int TABLE_START_COL = 5;
    public const int CONSOLE_LEFT_POS = 5;
    public const int CONSOLE_RIGHT_PAD = 35;
    public const int DATA_START = 6; //"data: ".Length;
    public const string DATA_JSON = "data: {";
    public const string EVENT_STATE = "event: state";
}
