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
	public int StartSession(string password)
	{
		if (Connection.Connect() == false)
			NotifyCommandError?.Invoke("Failed initializing TCP connection to ZKTeco device.");

		_ = SendCommand(Commands.Connect);
		_ = SendCommand(Commands.Authenticate, 8, password);

		return Connection.ConnectionId;
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
			NotifyCommandError?.Invoke("");

		return Connection.ReceiveData(length);
	}
}
