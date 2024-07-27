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
    public static bool LogToFile {  get; set; }

    //try reconnect if no activity after X
    public static async Task MonitorConnectionTimeoutAsync(CancellationToken token)
    {
        GlobalVariable.Servers.AsParallel().ForAll(async x =>
        {
            EState lastState = EState.Unknown;

            x.LastActivity = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
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
                 server.LastActivity.Elapsed.TotalSeconds >= server.ServerTimeOut)
            {
                server.CancellationTokenSource.Cancel();
                server.LastActivity.Restart();
            }
        }

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
            await HandleErrorAsync(e, server);
        }
    }

    private static async Task MonitorAsync(Server server, CancellationToken token)
    {
        int readPerSecond = 0;
        int numberOfNull = 0;
        var watchPerSecond = Stopwatch.StartNew();
        var watchNull = Stopwatch.StartNew();
        bool handleNext = false;

        using var client = new HttpClient();
        using var stream = await client.GetStreamAsync(server.Uri);
        using var reader = new StreamReader(stream);

        server.State = EState.Running;
        while (!token.IsCancellationRequested)
        {
            readPerSecond++;

            string data = await reader.ReadLineAsync().WaitAsync(server.CancellationTokenSource.Token);

            ThrowIfIssue(server, data, ref watchPerSecond, ref readPerSecond, ref watchNull, ref numberOfNull);

            if (handleNext)
            {
                await HandleEventAsync(data, server);
            }

            handleNext = string.Equals(data, Constant.EVENT_STATE, StringComparison.OrdinalIgnoreCase);

            if(LogToFile)
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
        if(data?.StartsWith(Constant.DATA_JSON) == true)
        {
            var json = JsonSerializer.Deserialize<Event>(data.AsSpan(Constant.DATA_START), GlobalVariable.JsonOptions);

            json.UnixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

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

    private static void ThrowIfIssue(Server server, string data, ref Stopwatch watchPerSecond, ref int readPerSecond, ref Stopwatch watchNull, ref int numberOfNull)
    {
        if (server.CancellationTokenSource.IsCancellationRequested)
        {
            throw new Exception($"{server.Name} CancellationTokenSource.IsCancellationRequested");
        }

        if (data == null)
        {
            numberOfNull++;
            Debug.WriteLine($"{DateTime.Now} {server.Name} data is null");
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
                throw new Exception($"{server.Name} readPerSecond, read {readPerSecond}, null {numberOfNull}");
            }

            watchPerSecond.Restart();
            readPerSecond = 0;
        }

        if (watchNull.ElapsedMilliseconds > 1000 * 60 * 2) // 2 minutes
        {
            if (numberOfNull > 1)
            {
                throw new Exception($"{server.Name} null, read {readPerSecond}, null {numberOfNull}");
            }

            watchNull.Restart();
            numberOfNull = 0;
        }
    }
}
