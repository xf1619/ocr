namespace ModbusConsoleApp.Models;

public sealed class PollDataEventArgs : EventArgs
{
    public required string Channel { get; init; }
    public required byte StationAddress { get; init; }
    public required ushort Address { get; init; }
    public required string RegisterName { get; init; }
    public required object Value { get; init; }
    public required DateTime Timestamp { get; init; }
}

public sealed class PollErrorEventArgs : EventArgs
{
    public required string Channel { get; init; }
    public required byte StationAddress { get; init; }
    public required ushort StartAddress { get; init; }
    public required ushort Quantity { get; init; }
    public required string Message { get; init; }
    public required DateTime Timestamp { get; init; }
}
