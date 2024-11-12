using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using esphomecsharp.Screen;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace esphomecsharp;

public static class EspHomeOperation
{
    public static bool LogToFile { get; set; }

    //try reconnect if no activity after X
    public static async Task MonitorConnectionTimeoutAsync(CancellationToken token)
    {
        GlobalVariable.Servers.AsParallel().ForAll(async server =>
        {
            EState lastState = EState.Unknown;

            server.LastActivity = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(5000);

                if (lastState != server.State)
                {
                    await Dashboard.PrintStateAsync(server.State, server.Row);

                    lastState = server.State;
                }

                if (!server.CancellationTokenSource.IsCancellationRequested &&
                     server.LastActivity.Elapsed.TotalSeconds >= server.ServerTimeOut)
                {
                    server.CancellationTokenSource.Cancel();
                    server.LastActivity.Restart();
                }
            }
        });

        await Task.CompletedTask;
    }

    public static async Task FetchDeviceDataAsync(CancellationToken token)
    {
        GlobalVariable.Servers.AsParallel().ForAll(async server =>
        {
            while (!token.IsCancellationRequested)
            {
                await TryMonitorAsync(server, token);
            }
        });

        await Task.CompletedTask;
    }

    private static async Task TryMonitorAsync(Server server, CancellationToken token)
    {
        server.CancellationTokenSource = new();
        try
        {
            //to make sure all events doesnt arrive at the same time
            await Task.Delay(Random.Shared.Next(0, 3000));

            PingReply pingReply = await PingAsync(server.Uri.Host);

            if (pingReply.Status == IPStatus.Success)
            {
                await MonitorAsync(server, token);
            }
            else
            {
                server.State = EState.Stopped;

                await Task.Delay(5000);
            }
        }
        catch (Exception e)
        {
            if (server.State != EState.Stopped)
            {
                await e.HandleErrorAsync(server.Name);
                server.State = EState.Stopped;
            }

            await Task.Delay(5000);
        }
    }

    private static async Task<PingReply> PingAsync(string host)
    {
        using var ping = new Ping();

        var pingReply = await ping.SendPingAsync(host, 1000);

        return pingReply;
    }

    private static async Task MonitorAsync(Server server, CancellationToken token)
    {
        bool handleNext = false;

        using var client = new HttpClient();
        using var stream = await client.GetStreamAsync(server.Uri);
        using var reader = new StreamReader(stream);

        server.State = EState.Running;
        while (!token.IsCancellationRequested)
        {
            string data = await reader.ReadLineAsync().WaitAsync(server.CancellationTokenSource.Token);

            if (server.CancellationTokenSource.IsCancellationRequested)
            {
                throw new Exception($"{server.Name} CancellationTokenSource.IsCancellationRequested");
            }

            //reader.EndOfStream cause deadlock if the device lose electricity or is disconnected
            if (data == null)
            {
                throw new Exception($"{server.Name} remote connection closed");
            }

            if (handleNext)
            {
                await HandleEventAsync(data, server);
            }

            handleNext = string.Equals(data, Constant.EVENT_STATE, StringComparison.OrdinalIgnoreCase);

            if (LogToFile)
            {
                var now = DateTime.Now;

                File.AppendAllText(server.Name + " - " + now.ToString("yyyy-MM-dd") + ".txt", now.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + " : " + (data ?? "<null>") + Environment.NewLine);
            }
        }
    }

    private static async Task HandleEventAsync(string data, Server server)
    {
        //basic check to see if the line could be json
        //if(data?.Length > GlobalVariable.DATA_START && data[GlobalVariable.DATA_START] == '{')
        if (data?.StartsWith(Constant.DATA_JSON) == true)
        {
            var json = JsonSerializer.Deserialize<Event>(data.AsSpan(Constant.DATA_START), GlobalVariable.JsonOptions);

            json.UnixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            await Header.TotalDailyEnergyAsync(json);

            await Header.TotalPowerAsync(json);

            await Dashboard.PrintRowAsync(server, json);
        }
    }
}
