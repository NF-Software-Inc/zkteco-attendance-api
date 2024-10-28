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
    public virtual IZkPacket? SendCommand(Commands command, string? data, int length = 8)
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

        if (Connection.SendData(packet) == false)
            NotifyCommandError?.Invoke("Failed sending data to ZKTeco device.");

        var response = Connection.ReceiveData(length);

        if (response == null || response.Length < ZkPacketBase.DefaultHeaderLength)
            return null;

        var result = ZkPacketBase.ParseHeader(response[..ZkPacketBase.DefaultHeaderLength]);

        if (response.Length > ZkPacketBase.DefaultHeaderLength)
            result.Data = response[ZkPacketBase.DefaultHeaderLength..];

        Connection.ResponseId = result.ResponseId;
        return result;
    }

    /// <inheritdoc cref="ICommand.SendCommand(Commands, byte[], int)" />
    public virtual IZkPacket? SendCommand(Commands command, byte[] data, int length = 8)
    {
        var packet = new TPacket()
        {
            Command = command,
            ConnectionId = Connection.ConnectionId,
            ResponseId = Connection.ResponseId,
            Data = data
        };

        if (Connection.SendData(packet) == false)
            NotifyCommandError?.Invoke("Failed sending data to ZKTeco device.");

        var response = Connection.ReceiveData(length);

        if (response == null || response.Length < ZkPacketBase.DefaultHeaderLength)
            return null;

        var result = ZkPacketBase.ParseHeader(response[..ZkPacketBase.DefaultHeaderLength]);

        if (response.Length > ZkPacketBase.DefaultHeaderLength)
            result.Data = response[ZkPacketBase.DefaultHeaderLength..];

        Connection.ResponseId = result.ResponseId;
        return result;
    }

    /// <inheritdoc cref="ICommand.SendBufferedCommand(Commands, byte[], int)" />
    public virtual IZkPacket? SendBufferedCommand(Commands command, byte[] data, int length)
    {
        var combined = new byte[] { 0x01 }.Concat(BitConverter.GetBytes((ushort)(int)command)).Concat(data).ToArray();
        var request = SendCommand(Commands.PrepareBuffers, combined, length);

        if (request == null || request.Command != Commands.Success || request.Data.Length < 5)
        {
            NotifyCommandError?.Invoke("Invalid response received during buffered read on ZKTeco device.");
            return null;
        }

        using var stream = new MemoryStream();

        var size = BitConverter.ToInt32(request.Data, 1);
        var start = 0;
        var first = true;

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

            var block = Connection.ReceiveBufferedData(current + ZkPacketBase.DefaultHeaderLength);

            if (first && block.Length > 0)
            {
                stream.Write(block);
                first = false;
            }
            else if (block.Length > 0)
            {
                stream.Write(block, 8, block.Length - 8);
            }

            size -= current;
            start += current;
        }

        var completed = stream.ToArray();
        var result = ZkPacketBase.ParseHeader(completed[..ZkPacketBase.DefaultHeaderLength]);

        if (completed.Length > ZkPacketBase.DefaultHeaderLength)
            result.Data = completed[ZkPacketBase.DefaultHeaderLength..];

        return result;
    }
}
