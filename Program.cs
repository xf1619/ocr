using System.Text.Json;
using ModbusConsoleApp.Models;
using ModbusConsoleApp.Services;

const string configPath = "appsettings.modbus.json";

var config = LoadOrCreateConfig(configPath);
var service = new ModbusPollingService(config);

service.DataReceived += (_, e) =>
{
    Console.WriteLine($"[{e.Timestamp:HH:mm:ss.fff}] DATA Channel={e.Channel}, Station={e.StationAddress}, Addr={e.Address}, Name={e.RegisterName}, Value={e.Value}");
};

service.PollError += (_, e) =>
{
    Console.WriteLine($"[{e.Timestamp:HH:mm:ss.fff}] ERROR Channel={e.Channel}, Station={e.StationAddress}, Frame=[{e.StartAddress}+{e.Quantity}], Msg={e.Message}");
};

service.PrintPlan();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Modbus 轮询启动，按 Ctrl+C 退出...");
await service.RunAsync(cts.Token);

static AppConfig LoadOrCreateConfig(string path)
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    if (!File.Exists(path))
    {
        var defaultConfig = AppConfig.CreateDefault();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, options));
        Console.WriteLine($"未发现配置文件，已生成模板: {path}");
        return defaultConfig;
    }

    var json = File.ReadAllText(path);
    var config = JsonSerializer.Deserialize<AppConfig>(json, options);

    return config ?? throw new InvalidOperationException($"配置解析失败: {path}");
}
