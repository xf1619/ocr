using ModbusConsoleApp.Models;

namespace ModbusConsoleApp.Services;

public static class FramePlanner
{
    public static List<ModbusFramePlan> BuildPlans(IEnumerable<RegisterConfig> registers, IReadOnlyDictionary<string, ChannelConfig> channels)
    {
        var result = new List<ModbusFramePlan>();

        var grouped = registers
            .GroupBy(r => new { r.Channel, r.StationAddress })
            .OrderBy(g => g.Key.Channel)
            .ThenBy(g => g.Key.StationAddress);

        foreach (var group in grouped)
        {
            if (!channels.TryGetValue(group.Key.Channel, out var channelConfig))
            {
                throw new InvalidOperationException($"Channel '{group.Key.Channel}' referenced by register but not defined.");
            }

            var ordered = group.OrderBy(r => r.Address).ToList();
            if (ordered.Count == 0)
            {
                continue;
            }

            var currentRegisters = new List<RegisterConfig> { ordered[0] };
            ushort frameStart = ordered[0].Address;
            ushort frameEnd = (ushort)(ordered[0].Address + ordered[0].RegisterLength - 1);

            for (var i = 1; i < ordered.Count; i++)
            {
                var register = ordered[i];
                var registerEnd = (ushort)(register.Address + register.RegisterLength - 1);
                var gap = register.Address > frameEnd ? register.Address - frameEnd - 1 : 0;
                var nextFrameEnd = Math.Max(frameEnd, registerEnd);
                var nextQuantity = nextFrameEnd - frameStart + 1;

                var shouldSplit = gap > channelConfig.MaxGap || nextQuantity > channelConfig.MaxRegistersPerFrame;

                if (shouldSplit)
                {
                    result.Add(CreatePlan(group.Key.Channel, group.Key.StationAddress, frameStart, frameEnd, currentRegisters));
                    currentRegisters = [register];
                    frameStart = register.Address;
                    frameEnd = registerEnd;
                    continue;
                }

                currentRegisters.Add(register);
                frameEnd = (ushort)nextFrameEnd;
            }

            result.Add(CreatePlan(group.Key.Channel, group.Key.StationAddress, frameStart, frameEnd, currentRegisters));
        }

        return result;
    }

    private static ModbusFramePlan CreatePlan(string channel, byte stationAddress, ushort start, ushort end, List<RegisterConfig> registers)
    {
        return new ModbusFramePlan
        {
            Channel = channel,
            StationAddress = stationAddress,
            StartAddress = start,
            Quantity = (ushort)(end - start + 1),
            Registers = [.. registers]
        };
    }
}
