using System.IO.Ports;

namespace ModbusConsoleApp.Models;

public sealed class ChannelConfig
{
    public required string Name { get; init; }
    public required string PortName { get; init; }
    public int BaudRate { get; init; } = 9600;
    public int DataBits { get; init; } = 8;
    public string Parity { get; init; } = "None";
    public string StopBits { get; init; } = "One";
    public int ReadTimeoutMs { get; init; } = 1000;
    public int PollIntervalMs { get; init; } = 500;
    public int MaxGap { get; init; } = 1;
    public int MaxRegistersPerFrame { get; init; } = 64;

    public Parity GetParity() => Enum.Parse<Parity>(Parity, true);
    public StopBits GetStopBits() => Enum.Parse<StopBits>(StopBits, true);
}
