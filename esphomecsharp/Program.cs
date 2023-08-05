using esphomecsharp;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

await EspHomeContext.CreateDBIfNotExistAsync();

await EspHomeOperation.MonitorConnectionTimeoutAsync();

await EspHomeOperation.FetchDeviceDataAsync();

var con = ConsoleOperation.RunAndProcessAsync();
var db = EspHomeContext.RunAndProcessAsync();

while (await ConsoleOperation.ReadKeyAsync());

EspHomeContext.StopQueue();
ConsoleOperation.StopQueue();

await Task.WhenAll(con, db);