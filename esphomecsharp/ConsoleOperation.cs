using esphomecsharp.Model;
using esphomecsharp.Screen;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace esphomecsharp;

public static class ConsoleOperation
{
    private static readonly BlockingCollection<ConsoleAction> Queue = new();

    public static async Task RunAndProcessAsync()
    {
        while (!Queue.IsCompleted)
        {
            try
            {
                await Task.Run(async () =>
                {
                    foreach (var item in Queue.GetConsumingEnumerable())
                    {
                        await item.PreAction();

                        await item.Action();

                        await item.PostAction();
                    }
                });
            }
            catch (Exception e)
            {
                await e.HandleErrorAsync("ConsoleOperation.RunAndProcess");

                await Task.Delay(5000);
            }
        }
    }

    public static void StopQueue()
    {
        Queue.CompleteAdding();
    }
    public static void AddQueue(EConsoleScreen screen, Func<Task> action)
    {
        Queue.Add(new() { Screen = screen, PreAction = ConsoleAction.NoOp, Action = action, PostAction = ConsoleAction.NoOp });
    }
    public static void AddQueue(EConsoleScreen screen, Func<Task> preAction, Func<Task> action)
    {
        Queue.Add(new() { Screen = screen, PreAction = preAction, Action = action, PostAction = ConsoleAction.NoOp });
    }

    public static void AddQueue(EConsoleScreen screen, Func<Task> preAction, Func<Task> action, Func<Task> postAction)
    {
        Queue.Add(new() { Screen = screen, PreAction = preAction, Action = action, PostAction = postAction });
    }

    public static async Task<bool> ReadKeyAsync()
    {
        var input = Console.ReadKey(true);

        switch(input.Key)
        {
            case ConsoleKey.F1:
                await ToggleCursorVisibilityAsync();
                break;
            case ConsoleKey.F2:
                await ClearHeaderAsync();
                break;
            case ConsoleKey.F3:
                await ReconnectServersAsync();
                break;
            case ConsoleKey.F4:
                return false;

            case ConsoleKey.F7:
                await SoftDeleteErrorAsync();
                break;
            case ConsoleKey.F8:
                await HardDeleteErrorAsync();
                break;
        }

        return true;
    }

    public static async Task ToggleCursorVisibilityAsync()
    {
        Console.CursorVisible = !Console.CursorVisible;

        await Task.CompletedTask;
    }

    public static async Task ReconnectServersAsync()
    {
        GlobalVariable.Servers.ForEach(x => x.CancellationTokenSource.Cancel());

        await Task.CompletedTask;
    }

    public static async Task SoftDeleteErrorAsync()
    {
        await EspHomeContext.SoftDeleteErrorAsync();
        await Header.PrintErrorAsync(true);
    }

    public static async Task HardDeleteErrorAsync()
    {
        await EspHomeContext.HardDeleteErrorAsync();
    }

    public static async Task ClearHeaderAsync()
    {
        AddQueue(EConsoleScreen.Header, async () =>
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("".PadRight(Console.WindowWidth * 4));

            await Task.CompletedTask;
        });

        await Header.PrintHelp();
    }
}
