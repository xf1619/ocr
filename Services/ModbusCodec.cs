using ModbusConsoleApp.Models;

namespace ModbusConsoleApp.Services;

public static class ModbusCodec
{
    public static ushort ComputeCrc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;

        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                var lsb = (crc & 0x0001) != 0;
                crc >>= 1;
                if (lsb)
                {
                    crc ^= 0xA001;
                }
            }
        }

        return crc;
    }

    public static byte[] BuildReadHoldingRegistersRequest(byte station, ushort startAddress, ushort quantity)
    {
        var frame = new byte[8];
        frame[0] = station;
        frame[1] = 0x03;
        frame[2] = (byte)(startAddress >> 8);
        frame[3] = (byte)(startAddress & 0xFF);
        frame[4] = (byte)(quantity >> 8);
        frame[5] = (byte)(quantity & 0xFF);

        var crc = ComputeCrc16(frame.AsSpan(0, 6));
        frame[6] = (byte)(crc & 0xFF);
        frame[7] = (byte)(crc >> 8);
        return frame;
    }

    public static ushort[] ParseReadHoldingRegistersResponse(byte station, ushort expectedQuantity, byte[] response)
    {
        if (response.Length < 5)
        {
            throw new InvalidOperationException("Response frame is too short.");
        }

        if (response[0] != station)
        {
            throw new InvalidOperationException($"Station mismatch. Expected {station}, got {response[0]}.");
        }

        if (response[1] == 0x83)
        {
            throw new InvalidOperationException($"Modbus exception code: 0x{response[2]:X2}");
        }

        if (response[1] != 0x03)
        {
            throw new InvalidOperationException($"Unsupported function code: 0x{response[1]:X2}");
        }

        var byteCount = response[2];
        var expectedBytes = expectedQuantity * 2;
        if (byteCount != expectedBytes)
        {
            throw new InvalidOperationException($"Byte count mismatch. Expected {expectedBytes}, got {byteCount}.");
        }

        var expectedLength = 3 + byteCount + 2;
        if (response.Length != expectedLength)
        {
            throw new InvalidOperationException($"Response length mismatch. Expected {expectedLength}, got {response.Length}.");
        }

        var crcReceived = (ushort)((response[^1] << 8) | response[^2]);
        var crcComputed = ComputeCrc16(response.AsSpan(0, response.Length - 2));
        if (crcReceived != crcComputed)
        {
            throw new InvalidOperationException("CRC validation failed.");
        }

        var registers = new ushort[expectedQuantity];
        for (var i = 0; i < expectedQuantity; i++)
        {
            var offset = 3 + i * 2;
            registers[i] = (ushort)((response[offset] << 8) | response[offset + 1]);
        }

        return registers;
    }

    public static object DecodeValue(RegisterConfig register, ushort[] frameRegisters, ushort frameStart)
    {
        var offset = register.Address - frameStart;
        var length = register.RegisterLength;
        var raw = frameRegisters.AsSpan(offset, length).ToArray();

        object value = register.DataType switch
        {
            RegisterDataType.Int16 => unchecked((short)raw[0]),
            RegisterDataType.UInt16 => raw[0],
            RegisterDataType.Int32 => (int)((raw[0] << 16) | raw[1]),
            RegisterDataType.UInt32 => ((uint)raw[0] << 16) | raw[1],
            RegisterDataType.Float32 => DecodeFloat(raw),
            RegisterDataType.Int64 => DecodeInt64(raw),
            RegisterDataType.UInt64 => DecodeUInt64(raw),
            RegisterDataType.Double64 => DecodeDouble(raw),
            _ => throw new ArgumentOutOfRangeException(nameof(register.DataType), register.DataType, "Unsupported data type")
        };

        return ApplyScale(value, register.Scale);
    }

    private static object ApplyScale(object value, double scale)
    {
        if (Math.Abs(scale - 1.0) < double.Epsilon)
        {
            return value;
        }

        return value switch
        {
            short v => v * scale,
            ushort v => v * scale,
            int v => v * scale,
            uint v => v * scale,
            long v => v * scale,
            ulong v => v * scale,
            float v => v * scale,
            double v => v * scale,
            _ => value
        };
    }

    private static float DecodeFloat(ushort[] words)
    {
        Span<byte> bytes = stackalloc byte[4];
        bytes[0] = (byte)(words[0] >> 8);
        bytes[1] = (byte)(words[0] & 0xFF);
        bytes[2] = (byte)(words[1] >> 8);
        bytes[3] = (byte)(words[1] & 0xFF);
        if (BitConverter.IsLittleEndian)
        {
            bytes.Reverse();
        }

        return BitConverter.ToSingle(bytes);
    }

    private static double DecodeDouble(ushort[] words)
    {
        Span<byte> bytes = stackalloc byte[8];
        for (var i = 0; i < 4; i++)
        {
            bytes[i * 2] = (byte)(words[i] >> 8);
            bytes[i * 2 + 1] = (byte)(words[i] & 0xFF);
        }
        if (BitConverter.IsLittleEndian)
        {
            bytes.Reverse();
        }

        return BitConverter.ToDouble(bytes);
    }

    private static long DecodeInt64(ushort[] words) => unchecked((long)DecodeUInt64(words));

    private static ulong DecodeUInt64(ushort[] words)
    {
        ulong value = 0;
        foreach (var word in words)
        {
            value = (value << 16) | word;
        }

        return value;
    }
}
