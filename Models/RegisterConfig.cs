namespace ModbusConsoleApp.Models;

public sealed class RegisterConfig
{
    public required string Name { get; init; }
    public required string Channel { get; init; }
    public byte StationAddress { get; init; }
    public ushort Address { get; init; }
    public RegisterDataType DataType { get; init; }
    public double Scale { get; init; } = 1.0;

    public ushort RegisterLength => DataType switch
    {
        RegisterDataType.Int16 or RegisterDataType.UInt16 => 1,
        RegisterDataType.Int32 or RegisterDataType.UInt32 or RegisterDataType.Float32 => 2,
        RegisterDataType.Int64 or RegisterDataType.UInt64 or RegisterDataType.Double64 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(DataType), DataType, "Unsupported data type")
    };
}

public enum RegisterDataType
{
    Int16,
    UInt16,
    Int32,
    UInt32,
    Float32,
    Int64,
    UInt64,
    Double64
}
