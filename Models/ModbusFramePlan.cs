namespace ModbusConsoleApp.Models;

public sealed class ModbusFramePlan
{
    public required string Channel { get; init; }
    public required byte StationAddress { get; init; }
    public required ushort StartAddress { get; init; }
    public required ushort Quantity { get; init; }
    public required List<RegisterConfig> Registers { get; init; } = [];

    public override string ToString()
    {
        return $"{Channel}/S{StationAddress} [{StartAddress}..{StartAddress + Quantity - 1}] => {Registers.Count} regs";
    }
}
