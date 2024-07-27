using esphomecsharp;
using esphomecsharp.Screen;
using System.Threading;
using System.Threading.Tasks;

CancellationTokenSource cts = new();

await EspHomeContext.CreateDBIfNotExistAsync();

await ConsolePeriodicTimer.StartTimerAsync(cts.Token);

await EspHomeOperation.MonitorConnectionTimeoutAsync(cts.Token);

await Header.PrintHelp();

await Dashboard.PrintTableAsync();

await EspHomeOperation.FetchDeviceDataAsync(cts.Token);

var con = ConsoleOperation.RunAndProcessAsync();
var db = EspHomeContext.RunAndProcessAsync();

while (await ConsoleOperation.ReadKeyAsync());

await cts.CancelAsync();

EspHomeContext.StopQueue();
ConsoleOperation.StopQueue();

await Task.WhenAll(con, db);