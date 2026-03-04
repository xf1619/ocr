using System.IO.Ports;
using ModbusConsoleApp.Models;

namespace ModbusConsoleApp.Services;

public sealed class ModbusRtuMaster : IDisposable
{
    private readonly SerialPort _port;

    public ModbusRtuMaster(ChannelConfig config)
    {
        _port = new SerialPort(config.PortName, config.BaudRate, config.GetParity(), config.DataBits, config.GetStopBits())
        {
            ReadTimeout = config.ReadTimeoutMs,
            WriteTimeout = config.ReadTimeoutMs
        };
    }

    public void Open()
    {
        if (!_port.IsOpen)
        {
            _port.Open();
        }
    }

    public ushort[] ReadHoldingRegisters(byte station, ushort startAddress, ushort quantity)
    {
        var request = ModbusCodec.BuildReadHoldingRegistersRequest(station, startAddress, quantity);
        _port.DiscardInBuffer();
        _port.Write(request, 0, request.Length);

        var responseLength = 3 + quantity * 2 + 2;
        var response = ReadExact(responseLength);
        return ModbusCodec.ParseReadHoldingRegistersResponse(station, quantity, response);
    }

    private byte[] ReadExact(int length)
    {
        var buffer = new byte[length];
        var offset = 0;

        while (offset < length)
        {
            var read = _port.Read(buffer, offset, length - offset);
            if (read <= 0)
            {
                throw new TimeoutException("No response bytes received.");
            }

            offset += read;
        }

        return buffer;
    }

    public void Dispose()
    {
        if (_port.IsOpen)
        {
            _port.Close();
        }
        _port.Dispose();
    }
}
