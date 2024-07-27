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
            GlobalVariable.FinalRows.TryGetValue($"{json.Id}{Constant.RES_TOTAL}", out RowInfo row) &&
            value != json.Data)
        {
            GlobalVariable.TotalDailyEnergy[json.Id] = json.Data;
            var total = GlobalVariable.TotalDailyEnergy.Sum(x => x.Value);

            ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS, 2);
                Console.Write($"{Constant.RES_TOTAL_DAILY_ENERGY} {total} {Constant.RES_KILLO_WATT}".PadRight(Constant.CONSOLE_RIGHT_PAD));

                await Task.CompletedTask;
            });

            if (GlobalVariable.InsertTotalDailyEnergy.Elapsed.TotalSeconds >= GlobalVariable.Settings.TotalInsertInterval)
            {
                await EspHomeContext.InsertTotalAsync(Constant.RES_KILLO_WATT, row, total);
                GlobalVariable.InsertTotalDailyEnergy.Restart();
            }
        }

        await Task.CompletedTask;
    }

    public static async Task TotalPowerAsync(Event json)
    {
        if (GlobalVariable.TotalPower.TryGetValue(json.Id, out decimal value) &&
            GlobalVariable.FinalRows.TryGetValue($"{json.Id}{Constant.RES_TOTAL}", out RowInfo row) &&
            value != json.Data)
        {
            GlobalVariable.TotalPower[json.Id] = json.Data;
            var total = GlobalVariable.TotalPower.Sum(x => x.Value);

            ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS, 3);
                Console.Write($"{Constant.RES_TOTAL_POWER} {total} {Constant.RES_WATT}".PadRight(Constant.CONSOLE_RIGHT_PAD));

                await Task.CompletedTask;
            });

            if (GlobalVariable.InsertTotalPower.Elapsed.TotalSeconds >= GlobalVariable.Settings.TotalInsertInterval)
            {
                await EspHomeContext.InsertTotalAsync(Constant.RES_WATT, row, total);
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

            Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS + Constant.CONSOLE_RIGHT_PAD + 1, 1);
            Console.Write($"{ConsoleOperation.Key.HideCursor} : Hide cursor     {ConsoleOperation.Key.HandleNextError} : Handle next error");

            Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS + Constant.CONSOLE_RIGHT_PAD + 1, 2);
            Console.Write($"{ConsoleOperation.Key.RefreshHeader} : Refresh header  {ConsoleOperation.Key.HandleAllErrors} : Handle all errors      {ConsoleOperation.Key.LogAllToFile}  : Log all to files");

            LogToFilePos = Console.CursorLeft + 2;
            Console.CursorTop++;

            Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS + Constant.CONSOLE_RIGHT_PAD + 1, 3);
            Console.Write($"{ConsoleOperation.Key.ReconnectAll} : Reconnect all   {ConsoleOperation.Key.DeleteAllHandledErrors} : Delete handled errors  {ConsoleOperation.Key.Quit} : Quit");

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
                Console.Write("!!!");
            }
            else
            {
                Console.Write("   ");
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
            Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS + Constant.CONSOLE_RIGHT_PAD + 1, 0);
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
                    Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS + Constant.CONSOLE_RIGHT_PAD - 2, 0);
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
                Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS + Constant.CONSOLE_RIGHT_PAD - 2, 0);
                Console.Write("".PadRight(message.Length + 3));

                await Task.CompletedTask;
            }
        });
    }
}
