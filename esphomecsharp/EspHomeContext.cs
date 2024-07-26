using esphomecsharp.EF;
using esphomecsharp.EF.Model;
using esphomecsharp.Model;
using esphomecsharp.Screen;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace esphomecsharp;

public static class EspHomeContext
{
    private static readonly BlockingCollection<(IDbItem dbItem, RowInfo rowInfo)> Queue = new();
    public static async Task RunAndProcessAsync()
    {
        while (!Queue.IsCompleted)
        {
            try
            {
                await Task.Run(async () =>
                {
                    try
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
                    }
                    catch (Exception e)
                    {
                        await e.HandleErrorAsync("EspHomeContext.RunAndProcess.Run");

                        await Task.Delay(5000);
                    }
                });
            }
            catch (Exception e)
            {
                await e.HandleErrorAsync("EspHomeContext.RunAndProcess");

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

            //WAL is needed since read and write at the same time can cause lock database exception
            await test.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");

            await test.Database.ExecuteSqlRawAsync("CREATE VIEW MinMaxValue as  \r\nSELECT  row.Name \r\n      , row.FriendlyName \r\n      , data.MaxValue \r\n      , data.MinValue \r\n      , row.Unit \r\nFROM [RowEntry] row \r\nINNER join \r\n( \r\n    SELECT  [RowEntryId] \r\n          , max([Data]) MaxValue\r\n          , min([Data]) MinValue\r\n    FROM [Event] \r\n    GROUP BY RowEntryId \r\n) data ON data.[RowEntryId] = row.[RowEntryId] \r\nORDER BY row.Unit, row.FriendlyName");

            await test.Database.ExecuteSqlRawAsync("CREATE VIEW ShowAll as  \r\nSELECT    datetime(data.UnixTime, 'unixepoch', 'localtime') DateTime \r\n         , date(data.UnixTime, 'unixepoch', 'localtime') Date \r\n         , time(data.UnixTime, 'unixepoch', 'localtime') Time \r\n         , data.UnixTime \r\n         , row.Name \r\n         , row.FriendlyName \r\n         , data.Data \r\n         , row.Unit \r\nFROM [RowEntry] row \r\nINNER join [Event] data ON data.[RowEntryId] = row.[RowEntryId] \r\nORDER BY row.FriendlyName, row.Name, data.UnixTime");

            await test.Database.CloseConnectionAsync();
        }
    }

    public static async Task GetDescIdAsync(Context EspHomeDb, Event json, RowInfo row)
    {
        if (row.DbDescId == null)
        {
            row.DbDescId = await EspHomeDb.RowEntry
                                        .Where(x => x.Name == json.Id &&
                                                    x.FriendlyName == row.Server.FriendlyName)
                                        .Select(x => x.RowEntryId)
                                        .FirstOrDefaultAsync();

            if (row.DbDescId == null)
            {
                var newId = new RowEntry()
                {
                    FriendlyName = row.Server.FriendlyName,
                    Name = json.Id,
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

    public static async Task InsertTotalAsync(string type, RowInfo row, decimal total)
    {
        var newJson = new Event()
        {
            Id = $"{row.Name}{GlobalVariable.RES_TOTAL}_{type}",
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

    public static async Task SoftDeleteNextErrorAsync()
    {
        using var EspHomeDb = new Context();

        var error = await EspHomeDb.Error.Where(x => !x.IsHandled).FirstOrDefaultAsync();

        if (error != null)
        {
            error.IsHandled = true;
            await EspHomeDb.SaveChangesAsync();
            _ = Header.ShowErrorAsync(error.Message);
        }
    }

    public static async Task SoftDeleteAllErrorAsync()
    {
        using var EspHomeDb = new Context();

        await EspHomeDb.Error.Where(x => !x.IsHandled).ExecuteUpdateAsync(x => x.SetProperty(p => p.IsHandled, v => true));
    }

    public static async Task HardDeleteAllHandledErrorAsync()
    {
        using var EspHomeDb = new Context();

        await EspHomeDb.Error.Where(x => x.IsHandled).ExecuteDeleteAsync();
    }
}
