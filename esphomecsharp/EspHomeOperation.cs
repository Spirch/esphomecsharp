using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace esphomecsharp
{
    public static class EspHomeOperation
    {
        public static bool Running { get; set; }

        //try reconnect if no activity after X
        public static async Task MonitorConnectionTimeoutAsync()
        {
            GlobalVariable.Servers.AsParallel().ForAll(async x =>
            {
                EState lastState = EState.Unknown;

                x.LastActivity = Stopwatch.StartNew();

                while (Running)
                {
                    await Task.Delay(10000);
                    
                    if(lastState != x.State) 
                    {
                        await ConsoleOperation.PrintStateAsync(x.State, x.Row);

                        lastState = x.State;
                    }

                    if(x.CancellationTokenSource != null) 
                    {
                        if (x.LastActivity.Elapsed.TotalSeconds > x.ServerTimeOut)
                        {
                            x.CancellationTokenSource.Cancel();
                            x.LastActivity.Restart();
                        }
                    }
                }
            });

            await Task.CompletedTask;
        }
        public static async Task FetchDeviceDataAsync()
        {
            GlobalVariable.Servers.AsParallel().ForAll(async x =>
            {
                while (Running)
                {
                    x.CancellationTokenSource = new();
                    int readPerSecond = 0;
                    var watchPerSecond = Stopwatch.StartNew();
                    bool showNext = false;
                    string data = "";
                    Event json = null;

                    try
                    {
                        //to make sure all events doesnt arrive at the same time
                        await Task.Delay(Random.Shared.Next(0, 5000));

                        using var client = new HttpClient();
                        using var stream = await client.GetStreamAsync(x.Uri);
                        using var reader = new StreamReader(stream);

                        x.State = EState.Running;
                        while (Running)
                        {
                            readPerSecond++;

                            data = await reader.ReadLineAsync().WaitAsync(x.CancellationTokenSource.Token);

                            if (x.CancellationTokenSource.IsCancellationRequested)
                            {
                                throw new Exception($"{x.Name} CancellationTokenSource.IsCancellationRequested");
                            }

                            if(data == null)
                            {
                                throw new Exception($"{x.Name} data is null");
                            }

                            //
                            //this cause deadlock if the device lose electricity or is disconnected
                            //
                            //if (reader.EndOfStream)
                            //{
                            //    throw new Exception($"{x.Name} EndOfStream");
                            //}

                            if (watchPerSecond.ElapsedMilliseconds > 1000)
                            {
                                if (readPerSecond > 100) //should never happen! but it did...
                                {
                                    throw new Exception($"{x.Name} readPerSecond");
                                }

                                watchPerSecond.Restart();
                                readPerSecond = 0;
                            }

                            if (showNext)
                            {
                                json = JsonSerializer.Deserialize<Event>(data.AsSpan(GlobalVariable.DATA_START), GlobalVariable.JsonOptions);

                                json.UnixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

                                await ConsoleOperation.PrintErrorAsync();

                                await ConsoleOperation.PrintTimeAsync();

                                await ConsoleOperation.TotalDailyEnergyAsync(json);

                                await ConsoleOperation.TotalPowerAsync(json);

                                await ConsoleOperation.PrintRowAsync(x, json);
                            }

                            showNext = string.Equals(data, GlobalVariable.EVENT_STATE, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    catch (Exception e)
                    {
                        if(x.State != EState.Stopped)
                        {
                            await ConsoleOperation.HandleErrorAsync(x.Name, e);
                            x.State = EState.Stopped;
                        }

                        await Task.Delay(5000);
                    }
                }
            });

            await Task.CompletedTask;
        }
    }
}
