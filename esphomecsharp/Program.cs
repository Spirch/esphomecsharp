using esphomecsharp;
using System.Threading.Tasks;

await EspHomeContext.CreateDBIfNotExistAsync();

EspHomeOperation.Running = true;
await EspHomeOperation.MonitorConnectionTimeoutAsync();

await ConsoleOperation.PrintTableAsync();

await EspHomeOperation.FetchDeviceDataAsync();

var con = ConsoleOperation.RunAndProcessAsync();
var db = EspHomeContext.RunAndProcessAsync();

await ConsoleOperation.PrintHelp();

while (await ConsoleOperation.ReadKeyAsync());

EspHomeOperation.Running = false;

EspHomeContext.StopQueue();
ConsoleOperation.StopQueue();

await Task.WhenAll(con, db);