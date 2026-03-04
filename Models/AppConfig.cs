namespace ModbusConsoleApp.Models;

public sealed class AppConfig
{
    public required List<ChannelConfig> Channels { get; init; } = [];
    public required List<RegisterConfig> Registers { get; init; } = [];

    public static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            Channels =
            [
                new ChannelConfig
                {
                    Name = "CH1",
                    PortName = "COM1",
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = "None",
                    StopBits = "One",
                    ReadTimeoutMs = 1000,
                    PollIntervalMs = 500,
                    MaxGap = 1,
                    MaxRegistersPerFrame = 32
                }
            ],
            Registers =
            [
                new RegisterConfig
                {
                    Name = "Temperature",
                    Channel = "CH1",
                    StationAddress = 1,
                    Address = 0,
                    DataType = RegisterDataType.Int16,
                    Scale = 0.1
                },
                new RegisterConfig
                {
                    Name = "Pressure",
                    Channel = "CH1",
                    StationAddress = 1,
                    Address = 1,
                    DataType = RegisterDataType.UInt16,
                    Scale = 0.01
                },
                new RegisterConfig
                {
                    Name = "Flow",
                    Channel = "CH1",
                    StationAddress = 1,
                    Address = 10,
                    DataType = RegisterDataType.Float32
                }
            ]
        };
    }
}
