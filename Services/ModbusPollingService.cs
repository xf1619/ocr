using ModbusConsoleApp.Models;

namespace ModbusConsoleApp.Services;

public sealed class ModbusPollingService
{
    private readonly AppConfig _config;
    private readonly Dictionary<string, ChannelConfig> _channelMap;
    private readonly Dictionary<string, List<ModbusFramePlan>> _plansByChannel;

    public event EventHandler<PollDataEventArgs>? DataReceived;
    public event EventHandler<PollErrorEventArgs>? PollError;

    public ModbusPollingService(AppConfig config)
    {
        _config = config;
        _channelMap = config.Channels.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var plans = FramePlanner.BuildPlans(config.Registers, _channelMap);
        _plansByChannel = plans
            .GroupBy(p => p.Channel, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    public void PrintPlan()
    {
        Console.WriteLine("=== 通讯帧规划 ===");
        foreach (var pair in _plansByChannel.OrderBy(p => p.Key))
        {
            Console.WriteLine($"通道: {pair.Key}");
            foreach (var plan in pair.Value)
            {
                Console.WriteLine($"  - {plan}");
            }
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var tasks = _config.Channels.Select(channel => RunChannelAsync(channel, cancellationToken)).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task RunChannelAsync(ChannelConfig channel, CancellationToken cancellationToken)
    {
        if (!_plansByChannel.TryGetValue(channel.Name, out var plans) || plans.Count == 0)
        {
            Console.WriteLine($"[{channel.Name}] 无寄存器配置，跳过。");
            return;
        }

        Console.WriteLine($"[{channel.Name}] 线程启动，端口={channel.PortName}");

        using var master = new ModbusRtuMaster(channel);

        try
        {
            master.Open();
        }
        catch (Exception ex)
        {
            RaiseError(channel.Name, 0, 0, 0, $"打开串口失败: {ex.Message}");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var plan in plans)
            {
                try
                {
                    var raw = master.ReadHoldingRegisters(plan.StationAddress, plan.StartAddress, plan.Quantity);
                    foreach (var register in plan.Registers)
                    {
                        var value = ModbusCodec.DecodeValue(register, raw, plan.StartAddress);
                        DataReceived?.Invoke(this, new PollDataEventArgs
                        {
                            Channel = plan.Channel,
                            StationAddress = plan.StationAddress,
                            Address = register.Address,
                            RegisterName = register.Name,
                            Value = value,
                            Timestamp = DateTime.Now
                        });
                    }
                }
                catch (TimeoutException ex)
                {
                    RaiseError(plan.Channel, plan.StationAddress, plan.StartAddress, plan.Quantity, $"超时: {ex.Message}");
                }
                catch (Exception ex)
                {
                    RaiseError(plan.Channel, plan.StationAddress, plan.StartAddress, plan.Quantity, ex.Message);
                }
            }

            try
            {
                await Task.Delay(channel.PollIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Console.WriteLine($"[{channel.Name}] 线程停止");
    }

    private void RaiseError(string channel, byte station, ushort startAddress, ushort quantity, string message)
    {
        PollError?.Invoke(this, new PollErrorEventArgs
        {
            Channel = channel,
            StationAddress = station,
            StartAddress = startAddress,
            Quantity = quantity,
            Message = message,
            Timestamp = DateTime.Now
        });
    }
}
