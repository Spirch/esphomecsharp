﻿using esphomecsharp.EF.Model;
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
                    Date = DateTime.Now.ToString(GlobalVariable.RES_DATE_TIME),
                    DeviceName = source,
                    Message = e.ToString(),
                });

                await PrintErrorAsync(true);
            });

            await Task.CompletedTask;
        }

        public static async Task PrintRowAsync(Server x, Event json)
        {
            if (GlobalVariable.FinalRows.TryGetValue(json.Id, out RowInfo row))
            {
                Queue.Add(() =>
                {
                    Console.ForegroundColor = x.Color;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS + row.Col, row.Row);
                    Console.Write($"{x.FriendlyName}: {row.Name} {json.State}".PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));
                });

                await EspHomeContext.InsertRowAsync(json, row);

                x.LastActivity.Restart();
            }
        }
        public static async Task PrintErrorAsync(bool force = false)
        {
            if (force || GlobalVariable.PrintError.ElapsedMilliseconds > 60000)
            {
                Queue.Add(async () =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 0);
                    var count = await EspHomeContext.GetErrorCountAsync();
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

            //if (!force)
            //{
            //    int val;

            //    if ((val = Random.Shared.Next(1, 100)) >= 98)
            //        throw new Exception($"val: {val}");
            //}

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
                    Console.Write(DateTime.Now.ToString(GlobalVariable.RES_DATE_TIME).PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));
                });

                GlobalVariable.PrintTime.Restart();
            }

            await Task.CompletedTask;
        }

        public static async Task TotalDailyEnergyAsync(Event json)
        {
            if (GlobalVariable.TotalDailyEnergy.ContainsKey(json.Id))
            {
                if (GlobalVariable.FinalRows.TryGetValue($"{json.Id}_total", out RowInfo row))
                {
                    if (double.TryParse(json.Value.ToString(), out double value))
                    {
                        GlobalVariable.TotalDailyEnergy[json.Id] = value;
                        var total = GlobalVariable.TotalDailyEnergy.Sum(x => x.Value);

                        Queue.Add(() =>
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 2);
                            Console.Write($"{GlobalVariable.RES_TOTAL_DAILY_ENERGY} {total.ToString(GlobalVariable.RES_DOUBLE_STRING)} {GlobalVariable.RES_KILLO_WATT}".PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));
                        });

                        await EspHomeContext.InsertTotalAsync(json, row, total, GlobalVariable.RES_KILLO_WATT);
                    }
                }
            }
            await Task.CompletedTask;
        }

        public static async Task TotalPowerAsync(Event json)
        {
            if (GlobalVariable.TotalPower.ContainsKey(json.Id))
            {
                if (GlobalVariable.FinalRows.TryGetValue($"{json.Id}_total", out RowInfo row))
                {
                    if (double.TryParse(json.Value.ToString(), out double value))
                    {
                        GlobalVariable.TotalPower[json.Id] = value;
                        var total = GlobalVariable.TotalPower.Sum(x => x.Value);

                        Queue.Add(() =>
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.SetCursorPosition(GlobalVariable.CONSOLE_LEFT_POS, 3);
                            Console.Write($"{GlobalVariable.RES_TOTAL_POWER} {total.ToString(GlobalVariable.RES_DOUBLE_STRING)} {GlobalVariable.RES_WATT}".PadRight(GlobalVariable.CONSOLE_RIGHT_PAD));
                        });

                        await EspHomeContext.InsertTotalAsync(json, row, total, GlobalVariable.RES_WATT);
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
            }

            await Task.CompletedTask;
        }
    }
}
