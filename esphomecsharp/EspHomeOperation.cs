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
using System.Threading.Tasks;

namespace esphomecsharp;

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
                await Task.Delay(5000);

                lastState = await CheckStateAsync(x, lastState);

                await CancelIfTimeoutAsync(x);
            }
        });

        await Task.CompletedTask;
    }

    private static async Task<EState> CheckStateAsync(Server server, EState lastState)
    {
        if (lastState != server.State)
        {
            await Dashboard.PrintStateAsync(server.State, server.Row);

            return server.State;
        }

        return lastState;
    }

    private static async Task CancelIfTimeoutAsync(Server server)
    {
        if (server.CancellationTokenSource != null)
        {
            if (!server.CancellationTokenSource.IsCancellationRequested &&
                 server.LastActivity.Elapsed.TotalSeconds > server.ServerTimeOut)
            {
                server.CancellationTokenSource.Cancel();
                server.LastActivity.Restart();
            }
        }

        await Task.CompletedTask;
    }

    public static async Task FetchDeviceDataAsync()
    {
        GlobalVariable.Servers.AsParallel().ForAll(async server =>
        {
            while (Running)
            {
                await TryMonitorAsync(server);
            }
        });

        await Task.CompletedTask;
    }

    private static async Task TryMonitorAsync(Server server)
    {
        server.CancellationTokenSource = new();
        try
        {
            //to make sure all events doesnt arrive at the same time
            await Task.Delay(Random.Shared.Next(0, 3000));

            PingReply pingReply = await PingAsync(server.Uri.Host);

            if (pingReply.Status == IPStatus.Success)
            {
                await MonitorAsync(server);
            }
            else
            {
                server.State = EState.Stopped;

                await Task.Delay(5000);
            }
        }
        catch (Exception e)
        {
            await HandleErrorAsync(e, server);
        }
    }

    private static async Task MonitorAsync(Server server)
    {
        int readPerSecond = 0;
        var watchPerSecond = Stopwatch.StartNew();
        bool handleNext = false;

        using var client = new HttpClient();
        using var stream = await client.GetStreamAsync(server.Uri);
        using var reader = new StreamReader(stream);

        server.State = EState.Running;
        while (Running)
        {
            readPerSecond++;

            string data = await reader.ReadLineAsync().WaitAsync(server.CancellationTokenSource.Token);

            ThrowIfIssue(server, data, ref watchPerSecond, ref readPerSecond);

            if (handleNext)
            {
                await HandleEventAsync(data, server);
            }

            handleNext = string.Equals(data, GlobalVariable.EVENT_STATE, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task HandleEventAsync(string data, Server server)
    {
        //basic check to see if the line could be json
        if(data?.Length > GlobalVariable.DATA_START && data[GlobalVariable.DATA_START] == '{')
        {
            //Debug.Print(data);

            var json = JsonSerializer.Deserialize<Event>(data.AsSpan(GlobalVariable.DATA_START), GlobalVariable.JsonOptions);

            json.UnixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            await Header.PrintErrorAsync();

            await Header.PrintTimeAsync();

            await Header.TotalDailyEnergyAsync(json);

            await Header.TotalPowerAsync(json);

            await Dashboard.PrintRowAsync(server, json);
        }
    }

    private static async Task<PingReply> PingAsync(string host)
    {
        using var ping = new Ping();

        var pingReply = await ping.SendPingAsync(host, 1000);

        return pingReply;
    }

    private static async Task HandleErrorAsync(Exception e, Server server)
    {
        if (server.State != EState.Stopped)
        {
            await e.HandleErrorAsync(server.Name);
            server.State = EState.Stopped;
        }

        await Task.Delay(5000);
    }

    private static void ThrowIfIssue(Server server, string data, ref Stopwatch watchPerSecond, ref int readPerSecond)
    {
        if (server.CancellationTokenSource.IsCancellationRequested)
        {
            throw new Exception($"{server.Name} CancellationTokenSource.IsCancellationRequested");
        }

        if (data == null)
        {
            throw new Exception($"{server.Name} data is null");
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
                throw new Exception($"{server.Name} readPerSecond");
            }

            watchPerSecond.Restart();
            readPerSecond = 0;
        }
    }
}
