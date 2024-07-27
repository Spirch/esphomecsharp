using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace esphomecsharp.Screen;

public sealed class Header
{
    public static async Task TotalDailyEnergyAsync(Event json)
    {
        if (GlobalVariable.TotalDailyEnergy.TryGetValue(json.Id, out decimal value) &&
            GlobalVariable.FinalRows.TryGetValue($"{json.Id}{GlobalVariable.RES_TOTAL}", out RowInfo row) &&
            value != json.Data)
        {
            GlobalVariable.TotalDailyEnergy[json.Id] = json.Data;
            var total = GlobalVariable.TotalDailyEnergy.Sum(x => x.Value);

            ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 2);
                Console.Write($"{GlobalVariable.RES_TOTAL_DAILY_ENERGY} {total} {GlobalVariable.RES_KILLO_WATT}".PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));

                await Task.CompletedTask;
            });

            if (GlobalVariable.InsertTotalDailyEnergy.Elapsed.TotalSeconds >= GlobalVariable.Settings.TotalInsertInterval)
            {
                await EspHomeContext.InsertTotalAsync(GlobalVariable.RES_KILLO_WATT, row, total);
                GlobalVariable.InsertTotalDailyEnergy.Restart();
            }
        }

        await Task.CompletedTask;
    }

    public static async Task TotalPowerAsync(Event json)
    {
        if (GlobalVariable.TotalPower.TryGetValue(json.Id, out decimal value) &&
            GlobalVariable.FinalRows.TryGetValue($"{json.Id}{GlobalVariable.RES_TOTAL}", out RowInfo row) &&
            value != json.Data)
        {
            GlobalVariable.TotalPower[json.Id] = json.Data;
            var total = GlobalVariable.TotalPower.Sum(x => x.Value);

            ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 3);
                Console.Write($"{GlobalVariable.RES_TOTAL_POWER} {total} {GlobalVariable.RES_WATT}".PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));

                await Task.CompletedTask;
            });

            if (GlobalVariable.InsertTotalPower.Elapsed.TotalSeconds >= GlobalVariable.Settings.TotalInsertInterval)
            {
                await EspHomeContext.InsertTotalAsync(GlobalVariable.RES_WATT, row, total);
                GlobalVariable.InsertTotalPower.Restart();
            }
        }

        await Task.CompletedTask;
    }

    private static int LogToFilePos;

    public static async Task PrintHelp()
    {
        ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD + 1, 1);
            Console.WriteLine($"{ConsoleOperation.Key.HideCursor} : Hide cursor     {ConsoleOperation.Key.HandleNextError} : Handle next error");

            Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD + 1, 2);
            Console.Write($"{ConsoleOperation.Key.RefreshHeader} : Refresh header  {ConsoleOperation.Key.HandleAllErrors} : Handle all errors      {ConsoleOperation.Key.LogAllToFile}  : Log all to files");

            LogToFilePos = Console.CursorLeft + 2;
            Console.WriteLine();

            Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD + 1, 3);
            Console.WriteLine($"{ConsoleOperation.Key.ReconnectAll} : Reconnect all   {ConsoleOperation.Key.DeleteAllHandledErrors} : Delete handled errors  {ConsoleOperation.Key.Quit} : Quit");

            await RefreshLogFlag();

            await Task.CompletedTask;
        });

        await Task.CompletedTask;
    }

    public static async Task RefreshLogFlag()
    {
        ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
        {
            Console.SetCursorPosition(LogToFilePos, 2);
            if (EspHomeOperation.LogToFile)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!!");
            }
            else
            {
                Console.WriteLine("   ");
            }

            await Task.CompletedTask;
        });

        await Task.CompletedTask;
    }

    private static Guid guid;
    public static async Task ShowErrorAsync(string message)
    {
        var localGuid = Guid.NewGuid();
        guid = localGuid;

        ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD + 1, 0);
            var leftBuffer = Console.WindowWidth - (message.Length + Console.CursorLeft);
            Console.Write(message.PadRight(leftBuffer));

            await Task.CompletedTask;
        });

        for (int i = 10; i > 0; i--)
        {
            await Task.Delay(1000);

            ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
            {
                if (guid == localGuid)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD - 2, 0);
                    Console.Write(i.ToString().PadLeft(2));

                    await Task.CompletedTask;
                }
            });

            if (guid != localGuid)
            {
                break;
            }
        }

        ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
        {
            if(guid == localGuid)
            {
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD - 2, 0);
                Console.Write("".PadRight(message.Length + 3));

                await Task.CompletedTask;
            }
        });
    }
}
