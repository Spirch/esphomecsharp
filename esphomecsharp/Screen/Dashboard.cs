using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using System;
using System.Threading.Tasks;

namespace esphomecsharp.Screen;

public sealed class Dashboard
{
    public static async Task PrintTableAsync()
    {
        foreach (var header in GlobalVariable.ColHeader)
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, async () =>
            {
                Console.ForegroundColor = header.Color;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + header.Col, header.Server.Row);
                Console.Write(header.Name.PadCenter(header.Padding));

                await Task.CompletedTask;
            });
        }

        foreach (var header in GlobalVariable.RowHeader)
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, async () =>
            {
                Console.ForegroundColor = header.Color;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + header.Col, header.Server.Row);
                Console.Write(header.Server.FriendlyName.PadLeft(header.Padding));

                await Task.CompletedTask;
            });
        }

        await Task.CompletedTask;
    }

    public static async Task PrintRowAsync(Server x, Event json)
    {
        if (GlobalVariable.FinalRows.TryGetValue(json.Id, out RowInfo row))
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, async () =>
            {
                row.LastPrint = json.State.PadCenter(row.Padding);

                await Task.CompletedTask;
            },
            async () =>
            {
                Console.ForegroundColor = x.Color;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + row.Col, row.Server.Row);
                Console.Write(row.LastPrint);

                await Task.CompletedTask;
            },
            async () =>
            {
                if (Math.Abs(json.Data - row.LastValue) >= row.RecordDelta || row.LastRecord >= row.RecordThrottle)
                {
                    row.LastValue = json.Data;
                    row.LastRecord = 0;
                    await EspHomeContext.InsertRowAsync(json, row);
                }
                else
                {
                    row.RecordThrottle++;
                }
            });


            x.LastActivity.Restart();
        }
        //else
        //{
        //    Debug.Print(json.Id);
        //}

        await Task.CompletedTask;
    }

    public static async Task PrintStateAsync(EState state, int row)
    {

        if (state == EState.Running)
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, async () =>
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                Console.WriteLine("@@@");

                await Task.CompletedTask;
            });
        }
        else if (state == EState.Stopped)
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, async () =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                Console.WriteLine("!!!");

                await Task.CompletedTask;
            });
        }
        else
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, async () =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                Console.WriteLine("???");

                await Task.CompletedTask;
            });
        }

        await Task.CompletedTask;
    }
}
