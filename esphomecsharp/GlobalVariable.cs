using esphomecsharp.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace esphomecsharp
{
    internal static class GlobalVariable
    {
        private sealed class Rows
        {
            public string Prefix { get; set; }
            public string Suffix { get; set; }
            public int Column { get; set; }
            public string Name { get; set; }
            public string Unit { get; set; }
            public bool IsTotalDailyEnergy { get; set; }
            public bool IsTotalPower { get; set; }
        }

        public const string RES_TOTAL_DAILY_ENERGY = "Total Daily Energy:";
        public const string RES_TOTAL_POWER = "Total Power:";
        public const string RES_TOTAL = "_total";
        public const string RES_NAME = "_name";

        public const int TABLE_START_COL = 5;

        public const string RES_KILLO_WATT = "kW";
        public const string RES_WATT = "W";
        public const string RES_DOUBLE_STRING = "0.00";
        public const string RES_DATE_TIME = "yyyy-MM-dd HH:mm:ss";

        public const int CONSOLE_LEFT_POS = 5;
        public const int CONSOLE_RIGHT_PAD = 35;
        public const int DATA_START = 6; //"data: ".Length;
        public const string EVENT_STATE = "event: state";

        public static readonly JsonSerializerOptions JsonOptions;

        public static readonly List<Server> Servers;
        public static readonly List<RowInfo> ColHeader;
        public static readonly List<RowInfo> RowHeader;
        public static readonly Dictionary<string, RowInfo> FinalRows;
        public static readonly int ServerNamePad;

        public static readonly Dictionary<string, double> TotalDailyEnergy;
        public static readonly Dictionary<string, double> TotalPower;

        public static readonly Stopwatch PrintTime;
        public static readonly Stopwatch PrintError;

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

            ServerNamePad = FinalRows.Values.Max(x => x.Name?.Length ?? 0) + 1;

            PrintTime = Stopwatch.StartNew();
            PrintError = Stopwatch.StartNew();
        }


        private static void InitRows(IConfigurationRoot settings, string page)
        {
            Servers.AddRange(settings.GetSection($"{page}-Servers").Get<List<Server>>());

            var rawRows = settings.GetSection($"{page}-RowInfo").Get<List<Rows>>();
            var rowTotalDailyEnergy = rawRows.Single(x => x.IsTotalDailyEnergy);
            var rowTotalPower = rawRows.Single(x => x.IsTotalPower);

            int serverPadding = Servers.Max(x => x.FriendlyName.Length) + 2;

            foreach (var server in Servers)
            {
                TotalDailyEnergy.Add($"{rowTotalDailyEnergy.Prefix}{server.Name}{rowTotalDailyEnergy.Suffix}", 0);

                TotalPower.Add($"{rowTotalPower.Prefix}{server.Name}{rowTotalPower.Suffix}", 0);

                RowHeader.Add(new RowInfo()
                {
                    Padding = serverPadding,
                    Name = server.FriendlyName,
                    Row = server.Row + TABLE_START_COL,
                    Color = server.Color,
                });
            }

            foreach (var total in TotalDailyEnergy)
            {
                FinalRows.Add($"{total.Key}{RES_TOTAL}", new RowInfo()
                {
                    Name = page,
                    FriendlyName = "Total Daily Energy",
                    Unit = RES_KILLO_WATT,
                });
            }

            foreach (var total in TotalPower)
            {
                FinalRows.Add($"{total.Key}{RES_TOTAL}", new RowInfo()
                {
                    Name = page,
                    FriendlyName = "Total Power",
                    Unit = RES_WATT,
                });
            }

            int rowPadding = rawRows.Max(x => x.Name.Length) + 2;

            foreach (var header in rawRows)
            {
                ColHeader.Add(new RowInfo()
                {
                    Padding = rowPadding,
                    Name = header.Name,
                    Row = TABLE_START_COL,
                    Col = header.Column * rowPadding,
                    Color = ConsoleColor.White,
                });
            }

           var rows = from r in rawRows
                   from s in Servers
                   select new
                   {
                       id = $"{r.Prefix}{s.Name}{r.Suffix}",
                       row = new RowInfo()
                       {
                           Padding = rowPadding,
                           Name = r.Name,
                           FriendlyName = s.FriendlyName,
                           Unit = r.Unit,

                           Row = s.Row + TABLE_START_COL,
                           Col = r.Column * rowPadding,
                       }
                   };

            foreach (var row in rows)
            {
                FinalRows.Add(row.id, row.row);
            }
        }
    }
}
