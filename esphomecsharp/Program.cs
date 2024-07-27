using esphomecsharp;
using esphomecsharp.Screen;
using System.Threading.Tasks;

_ = ConsolePeriodicTimer.StartPrintTimeAsync();
_ = ConsolePeriodicTimer.StartPrintErrorAsync();

await EspHomeContext.CreateDBIfNotExistAsync();

EspHomeOperation.Running = true;
await EspHomeOperation.MonitorConnectionTimeoutAsync();

await Header.PrintHelp();

await Dashboard.PrintTableAsync();

await EspHomeOperation.FetchDeviceDataAsync();

var con = ConsoleOperation.RunAndProcessAsync();
var db = EspHomeContext.RunAndProcessAsync();

while (await ConsoleOperation.ReadKeyAsync()) ;

EspHomeOperation.Running = false;

await ConsolePeriodicTimer.StopTimerAsync();

EspHomeContext.StopQueue();
ConsoleOperation.StopQueue();

await Task.WhenAll(con, db);