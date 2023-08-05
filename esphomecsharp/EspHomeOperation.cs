using esphomecsharp.EF.Model;
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
        public static bool Running;

        //reconnect if no activity after X
        //(hardcoded for now to 60 seconds, my device got an interval of 30s)
        public static async Task MonitorConnectionTimeoutAsync()
        {
            GlobalVariable.Servers.AsParallel().ForAll(async x =>
            {
                x.LastActivity = Stopwatch.StartNew();

                while (Running)
                {
                    await Task.Delay(5000);

                    if (x.LastActivity.Elapsed.TotalSeconds > 60)
                    {
                        x.CancellationTokenSource.Cancel();
                        x.LastActivity.Restart();
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

                    try
                    {
                        //to make sure all events doesnt arrive at the same time
                        await Task.Delay(Random.Shared.Next(0, 5000));

                        bool showNext = false;
                        string data = "";
                        Event json = null;

                        using var client = new HttpClient();
                        using var stream = await client.GetStreamAsync(x.Uri);
                        using var reader = new StreamReader(stream);

                        while (Running)
                        {
                            data = await reader.ReadLineAsync().WaitAsync(x.CancellationTokenSource.Token).ConfigureAwait(false);

                            if (showNext)
                            {
                                json = JsonSerializer.Deserialize<Event>(data.AsSpan(GlobalVariable.DATA_START), GlobalVariable.JsonOptions);

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
                        await ConsoleOperation.HandleErrorAsync(x.Name, e);

                        await Task.Delay(5000);
                    }
                }
            });

            await Task.CompletedTask;
        }
    }
}
