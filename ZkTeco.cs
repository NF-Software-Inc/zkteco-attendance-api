using System.Net;

namespace zkteco_attendance_api;

/// <summary>
/// Main class to communicate with ZKTeco devices.
/// </summary>
public class ZkTeco
{
	private readonly IConnection Connection;
	private readonly ICommand Command;

	/// <summary>
	/// Prepares the class for communications with a ZKTeco device.
	/// </summary>
	/// <param name="ip">The IP address of the ZKTeco device.</param>
	/// <param name="port">The port the ZKTeco device uses for communication.</param>
	/// <param name="useTcp">Spcifies whether to use TCP or UDP communication.</param>
	public ZkTeco(string ip, int port = 4_370, bool useTcp = true)
	{
		var address = IPAddress.Parse(ip);

		if (useTcp)
		{
			var connection = new TcpConnection(address, port);

			Connection = connection;
			Command = new TcpCommand(connection);
		}
		else
		{
			var connection = new UdpConnection(address, port);

			Connection = connection;
			Command = new UdpCommand(connection);
		}

		Connection.NotifyReceivedData += Connection_NotifyReceivedData;
		Connection.NotifySentData += Connection_NotifySentData;
		Command.NotifyCommandError += Command_NotifyCommandError;
	}

	/// <summary>
	/// Prepares the class for communications with a ZKTeco device.
	/// </summary>
	/// <param name="ip">The IP address of the ZKTeco device.</param>
	/// <param name="port">The port the ZKTeco device uses for communication.</param>
	/// <param name="useTcp">Spcifies whether to use TCP or UDP communication.</param>
	public ZkTeco(IPAddress ip, int port = 4_370, bool useTcp = true)
	{
		if (useTcp)
		{
			var connection = new TcpConnection(ip, port);

			Connection = connection;
			Command = new TcpCommand(connection);
		}
		else
		{
			var connection = new UdpConnection(ip, port);

			Connection = connection;
			Command = new UdpCommand(connection);
		}

		Connection.NotifyReceivedData += Connection_NotifyReceivedData;
		Connection.NotifySentData += Connection_NotifySentData;
		Command.NotifyCommandError += Command_NotifyCommandError;
	}

	/// <summary>
	/// Disconnects from the ZKTeco device.
	/// </summary>
	~ZkTeco()
	{
		Connection.NotifyReceivedData -= Connection_NotifyReceivedData;
		Connection.NotifySentData -= Connection_NotifySentData;
		Command.NotifyCommandError -= Command_NotifyCommandError;

		if (Connection.IsConnected)
			Connection.Disconnect();
	}

	/// <inheritdoc cref="IConnection.NotifySentData" />
	public event SentData? NotifySentData;

	/// <inheritdoc cref="IConnection.NotifyReceivedData" />
	public event ReceivedData? NotifyReceivedData;

	/// <inheritdoc cref="ICommand.NotifyCommandError" />
	public event CommandError? NotifyCommandError;

	private void Connection_NotifySentData(byte[] sent) => NotifySentData?.Invoke(sent);
	private void Connection_NotifyReceivedData(byte[] received) => NotifyReceivedData?.Invoke(received);
	private void Command_NotifyCommandError(string message) => NotifyCommandError?.Invoke(message);

	/// <summary>
	/// Opens a connection with the ZKTeco device.
	/// </summary>
	/// <param name="password">The password to authenticate with the ZKTeco device.</param>
	public bool Connect(int password = 0) => Connection.IsConnected || Command.StartSession(password);

	/// <summary>
	/// Closes the connection with the ZKTeco device.
	/// </summary>
	public bool Disconnect() => Connection.IsConnected == false || (Command.StopSession() && Connection.Disconnect());
}
