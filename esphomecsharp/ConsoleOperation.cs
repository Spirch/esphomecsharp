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

        await ToggleCursorVisibilityAsync(input);

        await ReconnectServersAsync(input);

        await ClearHeaderAsync(input);

        return input.KeyChar != 'q';
    }

    public static async Task ToggleCursorVisibilityAsync(ConsoleKeyInfo input)
    {
        if (input.KeyChar == 'i')
        {
            Console.CursorVisible = !Console.CursorVisible;
        }

        await Task.CompletedTask;
    }

    public static async Task ReconnectServersAsync(ConsoleKeyInfo input)
    {
        if (input.KeyChar == 'r')
        {
            GlobalVariable.Servers.ForEach(x => x.CancellationTokenSource.Cancel());
        }

        await Task.CompletedTask;
    }

    public static async Task ClearHeaderAsync(ConsoleKeyInfo input)
    {
        if (input.KeyChar == 'c')
        {
            AddQueue(EConsoleScreen.Header, async () =>
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("".PadRight(Console.WindowWidth * 4));

                await Task.CompletedTask;
            });

            await Header.PrintHelp();
        }

        await Task.CompletedTask;
    }
}
