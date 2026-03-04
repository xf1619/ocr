# Modbus .NET 控制台轮询程序

一个基于 **Modbus RTU** 的 .NET 控制台示例，支持：

1. 通道配置（串口、波特率、校验、停止位等）。
2. 寄存器配置（通道、站地址、寄存器地址、数据类型、缩放系数）。
3. 通讯帧优化（将多个相邻或间隔较小的寄存器合并为一帧读取）。
4. 按通道创建独立通讯线程（Task），循环轮询并通过事件上报数据或错误。

## 配置文件

程序启动时会读取当前目录的 `appsettings.modbus.json`：

- 若不存在，会自动生成默认模板。
- 可按现场设备修改通道和寄存器清单。

关键字段：

- `channels[*].maxGap`：两个寄存器地址可合并进同一帧的最大间隔。
- `channels[*].maxRegistersPerFrame`：单帧最大读取寄存器数量（避免帧过大）。

## 运行

```bash
dotnet run
```

按 `Ctrl + C` 退出。

## 事件

- `DataReceived`：读取成功后逐寄存器触发，回传地址、名称、解析值。
- `PollError`：超时/异常时触发，回传通道、站地址、帧范围、错误信息。

## 说明

当前示例实现的是 `03 Read Holding Registers`。
如需扩展写入功能（06/16）或 TCP，可在 `Services/ModbusRtuMaster.cs` 和 `Services/ModbusCodec.cs` 基础上扩展。
