using System.Text;

namespace zkteco_attendance_api;

/// <summary>
/// Implementation of <see cref="ICommand"/> to facilitate UDP based interactions.
/// </summary>
internal class UdpCommand(UdpConnection connection) : ICommand
{
	private readonly UdpConnection Connection = connection;

	/// <inheritdoc/>
	public event CommandError? NotifyCommandError;

	/// <inheritdoc/>
	public bool StartSession(int password)
	{
		if (Connection.Connect() == false)
			NotifyCommandError?.Invoke("Failed initializing UDP connection to ZKTeco device.");

		_ = SendCommand(Commands.Connect);
		_ = SendCommand(Commands.Authenticate, 8, password);

		return true;
	}

	/// <inheritdoc/>
	public byte[]? SendCommand(Commands command, int length = 8, string? data = null)
	{
		var packet = new UdpPacket()
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
