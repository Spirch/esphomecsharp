using esphomecsharp.Model;
using esphomecsharp.Screen;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace esphomecsharp;

public static class GlobalVariable
{
    public static readonly JsonSerializerOptions JsonOptions;

    public static readonly List<Server> Servers;
    public static readonly List<RowInfo> ColHeader;
    public static readonly List<RowInfo> RowHeader;
    public static readonly Dictionary<string, RowInfo> FinalRows;

    public static readonly Dictionary<string, decimal> TotalDailyEnergy;
    public static readonly Dictionary<string, decimal> TotalPower;

    public static readonly Stopwatch InsertTotalDailyEnergy;
    public static readonly Stopwatch InsertTotalPower;

    public static readonly Settings Settings;

    static GlobalVariable()
    {
        var settings = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .Build();
        Settings = settings.GetSection("Settings").Get<Settings>();

        JsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
        };

        FinalRows = new();
        TotalDailyEnergy = new();
        TotalPower = new();
        Servers = new();
        ColHeader = new();
        RowHeader = new();

        var page1 = Settings.Pages.Values.FirstOrDefault();

        InitRows(settings, page1);

        //to do periodic timer
        InsertTotalDailyEnergy = Stopwatch.StartNew();
        InsertTotalPower = Stopwatch.StartNew();
    }

    private static void InitRows(IConfigurationRoot settings, string page)
    {
        Servers.AddRange(settings.GetSection($"{page}-Servers").Get<List<Server>>());

        var rawRows = settings.GetSection($"{page}-RowInfo").Get<List<AppSettingsRowInfo>>();
        var rowTotalDailyEnergy = rawRows.Single(x => x.IsTotalDailyEnergy);
        var rowTotalPower = rawRows.Single(x => x.IsTotalPower);

        int serverPadding = Servers.Max(x => x.FriendlyName.Length);

        foreach (var server in Servers)
        {
            TotalDailyEnergy.Add($"{rowTotalDailyEnergy.Prefix}{server.Name}{rowTotalDailyEnergy.Suffix}", 0);

            TotalPower.Add($"{rowTotalPower.Prefix}{server.Name}{rowTotalPower.Suffix}", 0);

            RowHeader.Add(new RowInfo()
            {
                Server = server,
                Padding = serverPadding,
                Color = server.Color,
            });

            server.Row += Constant.TABLE_START_COL;
        }

        foreach (var total in TotalDailyEnergy)
        {
            FinalRows.Add($"{total.Key}{Constant.RES_TOTAL}", new RowInfo()
            {
                Server = new()
                {
                    FriendlyName = "Total Daily Energy",
                },
                Name = page,
                Unit = Constant.RES_KILLO_WATT,
            });
        }

        foreach (var total in TotalPower)
        {
            FinalRows.Add($"{total.Key}{Constant.RES_TOTAL}", new RowInfo()
            {
                Server = new()
                {
                    FriendlyName = "Total Power",
                },
                Name = page,
                Unit = Constant.RES_WATT,
            });
        }

        int ColPos = serverPadding + 1;
        int width;

        foreach (var header in rawRows)
        {
            width = Math.Max(header.Width, header.Name.Length + 2);
            ColHeader.Add(new RowInfo()
            {
                Server = new()
                {
                    Row = Constant.TABLE_START_COL,
                },
                Padding = width,
                Name = header.Name,
                Col = ColPos,
                Color = ConsoleColor.White,
                Hidden = header.Hidden,
            });

            ColPos += width;
        }

        var rows = from r in rawRows
                   join h in ColHeader on r.Name equals h.Name
                   from s in Servers
                   select new
                   {
                       id = $"{r.Prefix}{s.Name}{r.Suffix}",
                       row = new RowInfo()
                       {
                           Server = s,
                           Padding = h.Padding,
                           Name = r.Name,
                           Unit = r.Unit,

                           Col = h.Col,

                           RecordDelta = r.RecordDelta,
                           RecordThrottle = r.RecordThrottle,
                           Hidden = r.Hidden,
                       }
                   };

        foreach (var row in rows)
        {
            FinalRows.Add(row.id, row.row);
            row.row.LastRecordSw = new Stopwatch();
        }
    }
}
