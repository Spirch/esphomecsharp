using esphomecsharp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace esphomecsharp;
public static class ConsolePeriodicTimer
{
    private static PeriodicTimer printError;
    private static PeriodicTimer printTime;
    private static readonly CancellationTokenSource cts = new();

    public static async Task StopTimerAsync()
    {
        await cts.CancelAsync();
    }

    public static async Task StartPrintTimeAsync()
    {
        //trying to be as close as possible to a zero second without millisecond
        while (DateTime.Now.Millisecond > 5) ;

        printTime = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await printTime.WaitForNextTickAsync(cts.Token))
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 1);
                Console.Write(DateTime.Now.ToString(GlobalVariable.Settings.DateTimeFormat).PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));

                await Task.CompletedTask;
            });
        }
    }

    public static async Task StartPrintErrorAsync()
    {
        //trying to be as close as possible to a zero second without millisecond
        while (DateTime.Now.Millisecond > 5) ;

        printError = new PeriodicTimer(TimeSpan.FromSeconds(GlobalVariable.Settings.ShowErrorInterval));
        while (await printError.WaitForNextTickAsync(cts.Token))
        {
            await PrintErrorAsync();
        }
    }

    public static async Task PrintErrorAsync()
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

        await Task.CompletedTask;
    }
}
