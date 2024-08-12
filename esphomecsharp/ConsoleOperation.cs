using esphomecsharp.Model;
using esphomecsharp.Screen;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace esphomecsharp;

public static class ConsoleOperation
{
    public static class Key
    {
        //modifier is none
        public const ConsoleKey HideCursor = ConsoleKey.F2;
        public const ConsoleKey RefreshHeader = ConsoleKey.F3;
        public const ConsoleKey ReconnectAll = ConsoleKey.F4;

        public const ConsoleKey HandleNextError = ConsoleKey.F6;
        public const ConsoleKey HandleAllErrors = ConsoleKey.F7;
        public const ConsoleKey DeleteAllHandledErrors = ConsoleKey.F8;

        public const ConsoleKey LogAllToFile = ConsoleKey.F9;
        public const ConsoleKey Quit = ConsoleKey.F12;

        //modifier is shift
        public const ConsoleKey Graph1Day = ConsoleKey.F4;
        public const int Graph1DayValue = 1;

        public const ConsoleKey Graph3Days = ConsoleKey.F5;
        public const int Graph3DaysValue = 3;

        public const ConsoleKey Graph7Days = ConsoleKey.F6;
        public const int Graph7DaysValue = 7;

        public const ConsoleKey Graph14Days = ConsoleKey.F7;
        public const int Graph14DaysValue = 14;

        public const ConsoleKey Graph30Days = ConsoleKey.F8;
        public const int Graph30DaysValue = 30;

        public const ConsoleKey GraphAll = ConsoleKey.F9;
        public const int GraphAllValue = 0;

    }

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

        switch(input.Modifiers)
        {
            case ConsoleModifiers.Shift:
                switch (input.Key)
                {
                    case Key.Graph1Day:
                        await GraphAsync(Key.Graph1DayValue);
                        break;
                    case Key.Graph3Days:
                        await GraphAsync(Key.Graph3DaysValue);
                        break;
                    case Key.Graph7Days:
                        await GraphAsync(Key.Graph7DaysValue);
                        break;
                    case Key.Graph14Days:
                        await GraphAsync(Key.Graph14DaysValue);
                        break;
                    case Key.Graph30Days:
                        await GraphAsync(Key.Graph30DaysValue);
                        break;
                    case Key.GraphAll:
                        await GraphAsync(Key.GraphAllValue);
                        break;
                }
                break;

            case ConsoleModifiers.None:
                switch (input.Key)
                {
                    case Key.HideCursor:
                        await ToggleCursorVisibilityAsync();
                        break;
                    case Key.RefreshHeader:
                        await ClearHeaderAsync();
                        break;
                    case Key.ReconnectAll:
                        await ReconnectServersAsync();
                        break;

                    case Key.HandleNextError:
                        await SoftDeleteNextErrorAsync();
                        break;
                    case Key.HandleAllErrors:
                        await SoftDeleteAllErrorAsync();
                        break;
                    case Key.DeleteAllHandledErrors:
                        await HardDeleteAllHandledErrorAsync();
                        break;
                    case Key.LogAllToFile:
                        await ToggleLogToFileAsync();
                        break;
                    case Key.Quit:
                        return false;
                }
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

    public static async Task SoftDeleteNextErrorAsync()
    {
        await EspHomeContext.SoftDeleteNextErrorAsync();
        await ConsolePeriodicTimer.PrintErrorAsync();
    }

    public static async Task SoftDeleteAllErrorAsync()
    {
        await EspHomeContext.SoftDeleteAllErrorAsync();
        await ConsolePeriodicTimer.PrintErrorAsync();
    }

    public static async Task HardDeleteAllHandledErrorAsync()
    {
        await EspHomeContext.HardDeleteAllHandledErrorAsync();
    }

    public static async Task GraphAsync(int days)
    {
        await EspHomeContext.GraphAsync(days);
    }

    public static async Task ToggleLogToFileAsync()
    {
        EspHomeOperation.LogToFile = !EspHomeOperation.LogToFile;

        await Header.RefreshLogFlag();

        await Task.CompletedTask;
    }

    public static async Task ClearHeaderAsync()
    {
        AddQueue(EConsoleScreen.Header, async () =>
        {
            Console.SetCursorPosition(0, 0);
            Console.Write("".PadRight(Console.WindowWidth * 4));

            await Task.CompletedTask;
        });

        await Header.PrintHelp();
    }
}
