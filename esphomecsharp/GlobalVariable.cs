using esphomecsharp.Model;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
            public int Row { get; set; }
            public string Name { get; set; }
            public string Unit { get; set; }
            public bool IsTotalDailyEnergy { get; set; }
            public bool IsTotalPower { get; set; }
        }

        public const string RES_TOTAL_DAILY_ENERGY = "Total Daily Energy:";
        public const string RES_TOTAL_POWER = "Total Power:";
        public const string RES_TOTAL = "_total";

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
        public static readonly Dictionary<string, RowInfo> FinalRows;
        public static readonly int ValueRightPad;

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

            var page1 = Settings.Pages.Values.FirstOrDefault();

            InitRows(settings, page1);

            ValueRightPad = FinalRows.Values.Max(x => x.Name?.Length ?? 0) + 1;

            PrintTime = Stopwatch.StartNew();
            PrintError = Stopwatch.StartNew();
        }


        private static void InitRows(IConfigurationRoot settings, string page)
        {
            Servers.AddRange( settings.GetSection($"{page}-Servers").Get<List<Server>>());

            var rawRows = settings.GetSection($"{page}-RowInfo").Get<List<Rows>>();
            var rowTotalDailyEnergy = rawRows.Single(x => x.IsTotalDailyEnergy);
            var rowTotalPower = rawRows.Single(x => x.IsTotalPower);

            foreach (var server in Servers)
            {
                TotalDailyEnergy.Add($"{rowTotalDailyEnergy.Prefix}{server.Name}{rowTotalDailyEnergy.Suffix}", 0);

                TotalPower.Add($"{rowTotalPower.Prefix}{server.Name}{rowTotalPower.Suffix}", 0);
            }

            foreach (var total in TotalDailyEnergy)
            {
                FinalRows.Add($"{total.Key}{RES_TOTAL}", new RowInfo()
                {
                    Unit = RES_KILLO_WATT,
                });
            }

            foreach (var total in TotalPower)
            {
                FinalRows.Add($"{total.Key}{RES_TOTAL}", new RowInfo()
                {
                    Unit = RES_KILLO_WATT,
                });
            }

            var rowServers = Servers.GroupBy(x => x.Column);
            var rowPosition = rawRows.Count + 2;
            int rowNum = 0;

            foreach (var servers in rowServers)
            {
                rowNum = 0;
                foreach (var server in servers)
                {
                    server.Position = rowNum++ * rowPosition;

                    FinalRows.Add($"{server.Name}{server.FriendlyName}", new RowInfo()
                    {
                        Row = server.Position + 5,
                        Col = server.Column * (CONSOLE_RIGHT_PAD + 1),
                    });
                }
            }

            var rows = (from r in rawRows
                        from s in Servers
                        select new
                        {
                            id = $"{r.Prefix}{s.Name}{r.Suffix}",
                            row = new RowInfo()
                            {
                                Name = r.Name,
                                Row = s.Position + r.Row + 5,
                                Col = s.Column * (CONSOLE_RIGHT_PAD + 1),
                                Unit = r.Unit
                            }
                        }
                    );

            foreach(var row in rows)
            {
                FinalRows.Add(row.id, row.row);
            }
        }
    }
}
