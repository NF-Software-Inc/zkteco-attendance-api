using System.Text;

namespace zkteco_attendance_api;

/// <summary>
/// Implementation of <see cref="ICommand"/> to facilitate TCP based interactions.
/// </summary>
internal class TcpCommand(TcpConnection connection) : ICommand
{
    private readonly TcpConnection Connection = connection;

    /// <inheritdoc/>
    public event CommandError? NotifyCommandError;

    /// <inheritdoc/>
    public byte[]? SendCommand(Commands command, string? data, int length = 8)
    {
        var packet = new TcpPacket()
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

        return Connection.ReceiveData(length);
    }

    /// <inheritdoc/>
    public byte[]? SendCommand(Commands command, byte[] data, int length = 8)
    {
        var packet = new TcpPacket()
        {
            Command = command,
            ConnectionId = Connection.ConnectionId,
            ResponseId = Connection.ResponseId,
            Data = data
        };

        if (Connection.SendData(packet) == false)
            NotifyCommandError?.Invoke("Failed sending data to ZKTeco device.");

        return Connection.ReceiveData(length);
    }
}
