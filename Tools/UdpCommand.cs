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

		var result = SendCommand(Commands.Connect);

		if (result == null)
		{
			NotifyCommandError?.Invoke("Failed starting session with ZKTeco device.");
			return false;
		}

		var header = ZkPacketBase.ParseHeader(result.Take(8).ToArray());

		Connection.ConnectionId = header.ConnectionId;

		if (header.Command != Commands.Unauthorized)
			return true;

		result = SendCommand(Commands.Authenticate, 8, Functions.GeneratePassword(password, Connection.ConnectionId));

		if (result == null)
		{
			NotifyCommandError?.Invoke("Failed requesting authentication with ZKTeco device.");
			return false;
		}

		header = ZkPacketBase.ParseHeader(result.Take(8).ToArray());

		if (header.Command == Commands.Success)
			return true;

		NotifyCommandError?.Invoke("Failed authenticating with ZKTeco device.");
		return false;
	}

	/// <inheritdoc/>
	public bool StopSession()
	{
		var result = SendCommand(Commands.Disconnect);

		if (result == null)
		{
			NotifyCommandError?.Invoke("Failed closing session with ZKTeco device.");
			return false;
		}

		return ZkPacketBase.ParseHeader(result.Take(8).ToArray()).Command == Commands.Success;
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
			NotifyCommandError?.Invoke("Failed sending data to ZKTeco device.");

		return Connection.ReceiveData(length);
	}

	/// <inheritdoc/>
	public byte[]? SendCommand(Commands command, int length, byte[] data)
	{
		var packet = new UdpPacket()
		{
			Command = command,
			ConnectionId = Connection.ConnectionId,
			Data = data
		};

		if (Connection.SendData(packet) == false)
			NotifyCommandError?.Invoke("Failed sending data to ZKTeco device.");

		return Connection.ReceiveData(length);
	}
}
