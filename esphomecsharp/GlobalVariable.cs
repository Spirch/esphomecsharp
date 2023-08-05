using esphomecsharp.Model;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        }

        public const string RES_TOTAL_DAILY_ENERGY = "Total Daily Energy:";
        public const string RES_TOTAL_POWER = "Total Power:";
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

        public static readonly Dictionary<string, double> TotalDailyEnergy;
        public static readonly Dictionary<string, double> TotalPower;

        public static readonly Stopwatch PrintTime;
        public static readonly Stopwatch PrintError;

        public static readonly string DBFileName;

        static GlobalVariable()
        {
            JsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            };

            TotalDailyEnergy = new();
            TotalPower = new();

            var settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .Build();

            DBFileName = settings.GetSection("settings").GetValue<string>("DBFileName");
            Servers = settings.GetSection("s31").Get<List<Server>>();
            var rows = settings.GetSection("RowInfo").Get<List<Rows>>();

            var rowServers = Servers.GroupBy(x => x.Column);
            var rowPosition = rows.Count + 1;
            int rowNum = 0;
            int idServer = 1;

            rows.ForEach(x => x.Row = rowNum++);

            foreach (var servers in rowServers)
            {
                rowNum = 0;
                foreach (var server in servers)
                {
                    server.Id = idServer++;
                    server.Position = rowNum++ * rowPosition;

                    TotalDailyEnergy.Add($"sensor-{server.Name}_daily_energy", 0);
                    TotalPower.Add($"sensor-{server.Name}_power", 0);
                }
            }

            FinalRows = (from r in rows
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
                        ).ToDictionary(k => k.id, v => v.row);

            foreach(var total in TotalDailyEnergy)
            {
                FinalRows.Add($"{total.Key}_total", new RowInfo()
                {
                    Unit = RES_KILLO_WATT,
                });
            }

            foreach (var total in TotalPower)
            {
                FinalRows.Add($"{total.Key}_total", new RowInfo()
                {
                    Unit = RES_KILLO_WATT,
                });
            }

            PrintTime = Stopwatch.StartNew();
            PrintError = Stopwatch.StartNew();
        }
    }
}
