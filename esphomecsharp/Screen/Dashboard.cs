using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace esphomecsharp.Screen;

public sealed class Dashboard
{
    public static async Task PrintTableAsync()
    {
        foreach (var header in GlobalVariable.ColHeader)
        {
            if(!header.Hidden)
            {
                ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, async () =>
                {
                    Console.ForegroundColor = header.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + header.Col, header.Server.Row);
                    Console.Write(header.Name.PadCenter(header.Padding));

                    await Task.CompletedTask;
                });
            }
        }

        foreach (var header in GlobalVariable.RowHeader)
        {
            if (!header.Hidden)
            {
                ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, async () =>
                {
                    Console.ForegroundColor = header.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + header.Col, header.Server.Row);
                    Console.Write(header.Server.FriendlyName.PadLeft(header.Padding));

                    await Task.CompletedTask;
                });
            }
        }

        await Task.CompletedTask;
    }

    public static async Task PrintRowAsync(Server x, Event json)
    {
        if (GlobalVariable.FinalRows.TryGetValue(json.Id, out RowInfo row))
        {
            ConsoleOperation.AddQueue(EConsoleScreen.Dashboard,
            async () =>
            {
                row.LastPrint = json.State.PadCenter(row.Padding);

                await Task.CompletedTask;
            },
            async () =>
            {
                if (!row.Hidden)
                {
                    Console.ForegroundColor = x.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + row.Col, row.Server.Row);
                    Console.Write(row.LastPrint);
                }

                await Task.CompletedTask;
            },
            async () =>
            {
                if (json.Event_Type == null)
                {
                    if (!row.LastRecordSw.IsRunning || Math.Abs(json.Data - row.LastValue) >= row.RecordDelta || row.LastRecordSw.Elapsed.TotalSeconds >= row.RecordThrottle)
                    {
                        if (row.LastValue != json.Data)
                        {
                            row.LastValue = json.Data;
                            await EspHomeContext.InsertRowAsync(json, row);
                        }

                        row.LastRecordSw.Restart();
                    }
                }
                else 
                {
                    await EspHomeContext.InsertRowAsync(json, row);
                }
            });

            x.LastActivity.Restart();
        }
        //else
        //{
        //    Debug.WriteLine(json.Id);
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
