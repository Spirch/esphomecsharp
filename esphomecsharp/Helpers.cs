using esphomecsharp.EF.Model;
using System;
using System.Threading.Tasks;

namespace esphomecsharp;

public static class Helpers
{
    public static string PadCenter(this string str, int length)
    {
        if (str == null)
        {
            return "<null>";
        }

        int spaces = length - str.Length;
        int padLeft = spaces / 2 + str.Length;
        return str.PadLeft(padLeft).PadRight(length);
    }

    public static async Task HandleErrorAsync(this Exception e, string source, string message = null)
    {
        await EspHomeContext.InsertErrorAsync(new Error()
        {
            Date = DateTime.Now.ToString(GlobalVariable.Settings.DateTimeFormat),
            DeviceName = source,
            Exception = e.ToString(),
            Message = message ?? e.Message
        });
    }
}
