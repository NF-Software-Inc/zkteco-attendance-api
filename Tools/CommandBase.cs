using System.Text;

namespace zkteco_attendance_api;

/// <summary>
/// Base class to share logic between command implementations.
/// </summary>
internal abstract class CommandBase<TPacket>(IConnection connection, int maxBufferSize) where TPacket : class, IZkPacket, new()
{
    protected readonly IConnection Connection = connection;
    protected readonly int MaxBufferSize = maxBufferSize;

    /// <inheritdoc cref="ICommand.NotifyCommandError" />
    public virtual event CommandError? NotifyCommandError;

    /// <inheritdoc cref="ICommand.SendCommand(Commands, string?, int)" />
    public virtual IZkPacket? SendCommand(Commands command, string? data, int length)
    {
        var packet = new TPacket()
        {
            Command = command,
            ConnectionId = Connection.ConnectionId,
            ResponseId = Connection.ResponseId
        };

        if (string.IsNullOrWhiteSpace(data) == false)
            packet.Data = Encoding.UTF8.GetBytes(data);

        if (int.IsOddInteger(packet.Data.Length))
            packet.Data = packet.Data.Concat([(byte)0x00]).ToArray();

        if (Connection.SendPacket(packet) == false)
            NotifyCommandError?.Invoke("Failed sending data to ZKTeco device.");

        var response = Connection.ReceivePacket(length);

        if (response == null || response.Length < ZkPacketBase.DefaultHeaderLength)
            return null;

        var result = ZkPacketBase.ParseHeader(response[..ZkPacketBase.DefaultHeaderLength]);

        if (response.Length > ZkPacketBase.DefaultHeaderLength)
            result.Data = response[ZkPacketBase.DefaultHeaderLength..];

        Connection.ResponseId = result.ResponseId;
        return result;
    }

    /// <inheritdoc cref="ICommand.SendCommand(Commands, byte[], int)" />
    public virtual IZkPacket? SendCommand(Commands command, byte[] data, int length)
    {
        var packet = new TPacket()
        {
            Command = command,
            ConnectionId = Connection.ConnectionId,
            ResponseId = Connection.ResponseId,
            Data = data
        };

        if (Connection.SendPacket(packet) == false)
            NotifyCommandError?.Invoke("Failed sending data to ZKTeco device.");

        var response = Connection.ReceivePacket(length);

        if (response == null || response.Length < ZkPacketBase.DefaultHeaderLength)
            return null;

        var result = ZkPacketBase.ParseHeader(response[..ZkPacketBase.DefaultHeaderLength]);

        if (response.Length > ZkPacketBase.DefaultHeaderLength)
            result.Data = response[ZkPacketBase.DefaultHeaderLength..];

        Connection.ResponseId = result.ResponseId;
        return result;
    }

    /// <inheritdoc cref="ICommand.SendBufferedCommand(Commands, byte[])" />
    public virtual IZkPacket? SendBufferedCommand(Commands command, byte[] data)
    {
        var valid = new Commands[] { Commands.Success, Commands.SendData };
        var combined = new byte[] { 0x01 }.Concat(BitConverter.GetBytes((ushort)(int)command)).Concat(data).ToArray();
        var request = SendCommand(Commands.PrepareBuffers, combined, 1_024);
        
        if (request == null || valid.Contains(request.Command) == false || (request.Command == Commands.Success && request.Data.Length < 5) || (request.Command == Commands.SendData && request.Data.Length < 4))
        {
            NotifyCommandError?.Invoke("Invalid response received during buffered read on ZKTeco device.");
            return null;
        }

        if (request.Command == Commands.Success)
            return GetBufferData(request.Data);
        else if (request.Command == Commands.SendData)
            return GetRemainingData(request.Data);
        else
            return null;
    }

    protected virtual IZkPacket GetRemainingData(byte[] data)
    {
        var total = BitConverter.ToInt32(data) + 4;
        var remaining = Array.Empty<byte>();

        if (total - data.Length > 0)
            remaining = Connection.ReceiveData(total - data.Length);

        return new ZkPacketBase()
        {
            Command = Commands.Success,
            ConnectionId = Connection.ConnectionId,
            ResponseId = Connection.ResponseId,
            Data = data.Concat(remaining).ToArray()
        };
    }

    protected virtual IZkPacket? GetBufferData(byte[] data)
    {
        using var stream = new MemoryStream();

        var size = BitConverter.ToInt32(data, 1);
        var start = 0;
        var final = Array.Empty<byte>();

        while (size > 0)
        {
            var current = size >= MaxBufferSize ? MaxBufferSize : size;
            var parameters = BitConverter.GetBytes(start).Concat(BitConverter.GetBytes(current)).ToArray();

            var chunk = SendCommand(Commands.ReadBuffer, parameters, 16);

            if (chunk == null || chunk.Command != Commands.PrepareData)
            {
                NotifyCommandError?.Invoke("Invalid chunk received during buffered read on ZKTeco device.");
                break;
            }

            var block = Connection.ReceiveBufferedPacket(current);

            if (block.Length == 0)
            {
                NotifyCommandError?.Invoke("Invalid data block received during buffered read on ZKTeco device.");
                break;
            }

            stream.Write(block);
            final = Connection.ReceivePacket(ZkPacketBase.DefaultHeaderLength);

            size -= current;
            start += current;
        }

        if (final.Length < 8)
        {
            NotifyCommandError?.Invoke("Invalid final packet received during buffered read on ZKTeco device.");
            return null;
        }

        var result = ZkPacketBase.ParseHeader(final);

        if (stream.Length > 0)
            result.Data = stream.ToArray();

        var packet = SendCommand(Commands.ClearBuffers, [], ZkPacketBase.DefaultHeaderLength);

        if (packet == null || packet.Command != Commands.Success)
            NotifyCommandError?.Invoke("Failed clearing buffer on ZKTeco device.");

        return result;
    }
}
