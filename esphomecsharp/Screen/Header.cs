using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace esphomecsharp.Screen;

public sealed class Header
{
    public static async Task PrintErrorAsync()
    {
        if (GlobalVariable.PrintError.Elapsed.TotalSeconds > GlobalVariable.Settings.ShowErrorInterval)
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
            {
                var count = await EspHomeContext.GetErrorCountAsync();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 0);
                if (count == 0)
                {
                    Console.Write("".PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));
                }
                else
                {
                    Console.Write($"{count} error(s)".PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));
                }
            });

            GlobalVariable.PrintError.Restart();
        }

        await Task.CompletedTask;
    }

    public static async Task PrintTimeAsync()
    {
        if (GlobalVariable.PrintTime.ElapsedMilliseconds > 1000)
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 1);
                Console.Write(DateTime.Now.ToString(GlobalVariable.Settings.DateTimeFormat).PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));

                await Task.CompletedTask;
            });

            GlobalVariable.PrintTime.Restart();
        }

        await Task.CompletedTask;
    }

    public static async Task TotalDailyEnergyAsync(Event json)
    {
        if (GlobalVariable.TotalDailyEnergy.ContainsKey(json.Id))
        {
            if (GlobalVariable.FinalRows.TryGetValue($"{json.Id}{GlobalVariable.RES_TOTAL}", out RowInfo row))
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
        }
        await Task.CompletedTask;
    }

    public static async Task TotalPowerAsync(Event json)
    {
        if (GlobalVariable.TotalPower.ContainsKey(json.Id))
        {
            if (GlobalVariable.FinalRows.TryGetValue($"{json.Id}{GlobalVariable.RES_TOTAL}", out RowInfo row))
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
        }

        await Task.CompletedTask;
    }

    public static async Task PrintHelp()
    {
        ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD + 1, 1);
            Console.WriteLine("i : hide cursor   r : reconnect all");
            Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD + 1, 2);
            Console.WriteLine("c : clear header  q : quit");

            await Task.CompletedTask;
        });

        await Task.CompletedTask;
    }
}
