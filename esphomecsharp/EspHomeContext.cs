using esphomecsharp.EF;
using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace esphomecsharp
{
    public static class EspHomeContext
    {
        private static readonly BlockingCollection<(IDbItem dbItem, RowInfo rowInfo)> Queue = new();
        public static async Task RunAndProcessAsync()
        {
            while(!Queue.IsCompleted)
            {
                try
                {
                    await Task.Run(async () =>
                    {
                        using var EspHomeDb = new Context();

                        foreach (var (dbItem, rowInfo) in Queue.GetConsumingEnumerable())
                        {
                            if (rowInfo != null && dbItem is Event json)
                            {
                                await GetDescIdAsync(EspHomeDb, json, rowInfo);
                            }

                            await EspHomeDb.AddAsync(dbItem);
                            await EspHomeDb.SaveChangesAsync();
                            EspHomeDb.ChangeTracker.Clear();
                        }
                    });
                }
                catch (Exception e)
                {
                    await ConsoleOperation.HandleErrorAsync("EspHomeContext.RunAndProcess", e);

                    await Task.Delay(5000);
                }
            }
        }

        public static void StopQueue()
        {
            Queue.CompleteAdding();
        }

        public static async Task CreateDBIfNotExistAsync()
        {
            if (!File.Exists(GlobalVariable.Settings.DBFileName))
            {
                using var test = new Context();

                test.Database.EnsureDeleted();
                test.Database.EnsureCreated();

                await test.Database.OpenConnectionAsync();
                await test.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE");
                await test.Database.CloseConnectionAsync();
            }
        }

        public static async Task GetDescIdAsync(Context EspHomeDb, Event json, RowInfo row)
        {
            if (row.DbDescId == null)
            {
                row.DbDescId = await EspHomeDb.RowEntry
                                            .Where(x => x.Value == json.Id)
                                            .Select(x => x.RowEntryId)
                                            .FirstOrDefaultAsync();

                if (row.DbDescId == null)
                {
                    var newId = new RowEntry()
                    {
                        Value = json.Id,
                        Unit = row.Unit,
                    };

                    await EspHomeDb.AddAsync(newId);
                    await EspHomeDb.SaveChangesAsync();

                    row.DbDescId = newId.RowEntryId;
                }
            }

            json.RowEntryId = row.DbDescId.Value;
        }

        public static async Task InsertErrorAsync(Error error)
        {
            Queue.Add((error, null));

            await Task.CompletedTask;
        }

        public static async Task InsertRowAsync(Event json, RowInfo row)
        {
            Queue.Add((json, row));

            await Task.CompletedTask;
        }

        public static async Task InsertTotalAsync(Event json, RowInfo row, double total, string unit)
        {
            var newJson = new Event()
            {
                Id = $"{json.Id}_total",
                Value = total,
                UnixTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
            };

            Queue.Add((newJson, row));

            await Task.CompletedTask;
        }

        public static async Task<int> GetErrorCountAsync()
        {
            using var EspHomeDb = new Context();

            return await EspHomeDb.Error.CountAsync(x => !x.IsHandled);
        }
    }
}
