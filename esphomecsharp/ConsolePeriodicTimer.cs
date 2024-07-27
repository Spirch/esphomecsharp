using esphomecsharp.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace esphomecsharp;
public static class ConsolePeriodicTimer
{
    public static async Task StartTimerAsync(CancellationToken token)
    {
        _ = StartPrintTimeAsync(token);
        _ = StartPrintErrorAsync(token);

        await Task.CompletedTask;
    }

    private static async Task StartPrintTimeAsync(CancellationToken token)
    {
        //trying to be as close as possible to a zero second without millisecond
        while (DateTime.Now.Millisecond > 5) ;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(token))
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Header, async () =>
            {

                //to do move to actual row of the server
                Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS + -4, 1);
                if (GlobalVariable.Servers.Any(x => x.State == EState.Running && x.LastActivity.Elapsed.TotalSeconds > x.ServerTimeOut))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("???");
                }
                else
                {
                    Console.Write("   ");
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS, 1);

                Console.Write(DateTime.Now.ToString(GlobalVariable.Settings.DateTimeFormat).PadRight(Constant.CONSOLE_RIGHT_PAD));

                await Task.CompletedTask;
            });
        }
    }

    private static async Task StartPrintErrorAsync(CancellationToken token)
    {
        //trying to be as close as possible to a zero second without millisecond
        while (DateTime.Now.Millisecond > 5) ;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(GlobalVariable.Settings.ShowErrorInterval));
        while (await timer.WaitForNextTickAsync(token))
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
            Console.SetCursorPosition(Constant.CONSOLE_LEFT_POS, 0);
            if (count == 0)
            {
                Console.Write("".PadRight(Constant.CONSOLE_RIGHT_PAD));
            }
            else
            {
                Console.Write($"{count} error(s)".PadRight(Constant.CONSOLE_RIGHT_PAD));
            }
        });

        await Task.CompletedTask;
    }
}
