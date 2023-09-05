using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using System;
using System.Collections;
using System.Collections.Generic;
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
                ConsoleOperation.AddQueue(() =>
                {
                    Console.ForegroundColor = header.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + header.Col, header.Server.Row);
                    Console.Write(header.Name.PadCenter(header.Padding));
                });
            }

            foreach (var header in GlobalVariable.RowHeader)
            {
                ConsoleOperation.AddQueue(() =>
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
                ConsoleOperation.AddQueue(() =>
                {
                    row.LastValue = json.State.PadCenter(row.Padding);

                    Console.ForegroundColor = x.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + row.Col, row.Server.Row);
                    Console.Write(row.LastValue);
                });

                await EspHomeContext.InsertRowAsync(json, row);

                x.LastActivity.Restart();
            }
        }

        public static async Task PrintStateAsync(EState state, int row)
        {

            if (state == EState.Running)
            {
                ConsoleOperation.AddQueue(() =>
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                    Console.WriteLine("@@@");
                });
            }
            else if (state == EState.Stopped)
            {
                ConsoleOperation.AddQueue(() =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                    Console.WriteLine("!!!");
                });
            }
            else
            {
                ConsoleOperation.AddQueue(() =>
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
