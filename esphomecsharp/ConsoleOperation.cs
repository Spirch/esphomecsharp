using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace esphomecsharp
{
    public static class ConsoleOperation
    {
        private static readonly BlockingCollection<Action> Queue = new();

        public static async Task RunAndProcessAsync()
        {
            while (!Queue.IsCompleted)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        foreach (var item in Queue.GetConsumingEnumerable())
                        {
                            item.Invoke();
                        }
                    });
                }
                catch (Exception e)
                {
                    await HandleErrorAsync("ConsoleOperation.RunAndProcess", e);

                    await Task.Delay(5000);
                }
            }
        }

        public static void StopQueue()
        {
            Queue.CompleteAdding();
        }

        public static async Task<bool> ReadKeyAsync()
        {
            var input = Console.ReadKey(true);

            await ToggleCursorVisibilityAsync(input);

            await ReconnectServersAsync(input);

            await ClearHeaderAsync(input);

            return input.KeyChar != 'q';
        }

        public static async Task HandleErrorAsync(string source, Exception e)
        {
            Queue.Add(async () =>
            {
                await EspHomeContext.InsertErrorAsync(new Error()
                {
                    Date = DateTime.Now.ToString(GlobalVariable.Settings.DateTimeFormat),
                    DeviceName = source,
                    Message = e.ToString(),
                });
            });

            await Task.CompletedTask;
        }

        public static async Task PrintTableAsync()
        {
            foreach(var header in GlobalVariable.ColHeader) 
            {
                Queue.Add(() =>
                {
                    Console.ForegroundColor = header.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + header.Col, header.Server.Row);
                    Console.Write(header.Name.PadCenter(header.Padding));
                });
            }

            foreach(var header in GlobalVariable.RowHeader)
            {
                Queue.Add(() =>
                {
                    Console.ForegroundColor = header.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + header.Col, header.Server.Row);
                    Console.Write(header.Server.FriendlyName.PadLeft(header.Padding));
                });
            }

            await PrintHelp();
        }

        public static async Task PrintRowAsync(Server x, Event json)
        {
            if (GlobalVariable.FinalRows.TryGetValue(json.Id, out RowInfo row))
            {
                Queue.Add(() =>
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

        public static async Task PrintErrorAsync()
        {
            if (GlobalVariable.PrintError.Elapsed.TotalSeconds > GlobalVariable.Settings.ShowErrorInterval)
            {
                Queue.Add(async () =>
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
                Queue.Add(() =>
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 1);
                    Console.Write(DateTime.Now.ToString(GlobalVariable.Settings.DateTimeFormat).PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));
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

                    Queue.Add(() =>
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 2);
                        Console.Write($"{GlobalVariable.RES_TOTAL_DAILY_ENERGY} {total} {GlobalVariable.RES_KILLO_WATT}".PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));
                    });

                        
                    if(GlobalVariable.InsertTotalDailyEnergy.Elapsed.TotalSeconds >= GlobalVariable.Settings.TotalInsertInterval)
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

                    Queue.Add(() =>
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 3);
                        Console.Write($"{GlobalVariable.RES_TOTAL_POWER} {total} {GlobalVariable.RES_WATT}".PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));
                    });

                    if(GlobalVariable.InsertTotalPower.Elapsed.TotalSeconds >= GlobalVariable.Settings.TotalInsertInterval)
                    {
                        await EspHomeContext.InsertTotalAsync(GlobalVariable.RES_WATT, row, total);
                        GlobalVariable.InsertTotalPower.Restart();
                    }
                }
            }

            await Task.CompletedTask;
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
                Queue.Add(() =>
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("".PadRight(Console.WindowWidth * 4));
                });

                await PrintHelp();
            }

            await Task.CompletedTask;
        }

        public static async Task PrintHelp()
        {
            Queue.Add(() =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD + 1, 1);
                Console.WriteLine("i : hide input    r : reconnect all");
                Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + GlobalVariable.CONSOLE_RIGHT_PAD + 1, 2);
                Console.WriteLine("c : clear header  q : quit");
            });

            await Task.CompletedTask;
        }

        public static async Task PrintStateAsync(EState state, int row)
        {

            if (state == EState.Running)
            {
                Queue.Add(() =>
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                    Console.WriteLine("@@@");
                });
            }
            else if (state == EState.Stopped)
            {
                Queue.Add(() =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                    Console.WriteLine("!!!");
                });
            }
            else
            {
                Queue.Add(() =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + -4, row);
                    Console.WriteLine("???");
                });
            }

            await Task.CompletedTask;
        }

        public static string PadCenter(this string str, int length)
        {
            int spaces = length - str.Length;
            int padLeft = spaces / 2 + str.Length;
            return str.PadLeft(padLeft).PadRight(length);
        }
    }
}
