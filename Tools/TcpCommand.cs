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
	public bool StartSession(int password)
	{
		if (Connection.Connect() == false)
			NotifyCommandError?.Invoke("Failed initializing TCP connection to ZKTeco device.");

		var result = SendCommand(Commands.Connect);

		if (result == null)
		{
			NotifyCommandError?.Invoke("Failed starting session with ZKTeco device.");
			return false;
		}

		var header = ZkPacketBase.ParseHeader(result.Take(8).ToArray());

		Connection.ConnectionId = header.ConnectionId;

		if (header.Command == Commands.Unauthorized)
		{
			_ = SendCommand(Commands.Authenticate, 8, password);
		}

		return true;
	}

	/// <inheritdoc/>
	public byte[]? SendCommand(Commands command, int length = 8, string? data = null)
	{
		var packet = new TcpPacket()
		{
			Command = command,
			ConnectionId = Connection.ConnectionId
		};

		if (string.IsNullOrWhiteSpace(data) == false)
			packet.Data = Encoding.UTF8.GetBytes(data);

		if (Connection.SendData(packet) == false)
			NotifyCommandError?.Invoke("Failed sending data to ZKTeco device.");

		return Connection.ReceiveData(length);
	}
}
