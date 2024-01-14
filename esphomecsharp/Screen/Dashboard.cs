using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace esphomecsharp.Screen
{
    public sealed class Dashboard
    {
        public static async Task PrintTableAsync()
        {
            foreach (var header in GlobalVariable.ColHeader)
            {
                ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, () =>
                {
                    Console.ForegroundColor = header.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + header.Col, header.Server.Row);
                    Console.Write(header.Name.PadCenter(header.Padding));
                });
            }

            foreach (var header in GlobalVariable.RowHeader)
            {
                ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, () =>
                {
                    Console.ForegroundColor = header.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + header.Col, header.Server.Row);
                    Console.Write(header.Server.FriendlyName.PadLeft(header.Padding));
                });
            }

            await Task.CompletedTask;
        }

        public static async Task PrintRowAsync(Server x, Event json)
        {
            if (GlobalVariable.FinalRows.TryGetValue(json.Id, out RowInfo row))
            {
                ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, () =>
                {
                    row.LastPrint = json.State.PadCenter(row.Padding);
                },
                () =>
                {
                    Console.ForegroundColor = x.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + row.Col, row.Server.Row);
                    Console.Write(row.LastPrint);
                },
                async () =>
                {
                    if(Math.Abs(json.Data - row.LastValue) >= row.RecordDelta || row.LastRecord >= row.RecordThrottle)
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
                ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, () =>
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                    Console.WriteLine("@@@");
                });
            }
            else if (state == EState.Stopped)
            {
                ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, () =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                    Console.WriteLine("!!!");
                });
            }
            else
            {
                ConsoleOperation.AddQueue(EConsoleScreen.Dashboard, () =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                    Console.WriteLine("???");
                });
            }

            await Task.CompletedTask;
        }
    }
}
