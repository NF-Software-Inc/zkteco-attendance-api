using System.Net;
using System.Net.Sockets;

namespace zkteco_attendance_api;

/// <summary>
/// Implementation of <see cref="IConnection"/> to facilitate TCP based connections.
/// </summary>
internal class TcpConnection : IConnection
{
	private const int CommandTimeoutMs = 2_000;
	private readonly Socket Socket;
	private readonly IPEndPoint Endpoint;

	public TcpConnection(IPAddress ip, int port)
	{
		Endpoint = new IPEndPoint(ip, port);

		Socket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
		{
			ReceiveTimeout = CommandTimeoutMs,
			SendTimeout = CommandTimeoutMs
		};
	}

	~TcpConnection()
	{
		Disconnect();
		Socket.Dispose();
	}

	/// <inheritdoc/>
	public bool IsConnected => Socket.Connected;

	/// <inheritdoc/>
	public int ConnectionId { get; set; }

	/// <inheritdoc/>
	public int ResponseId { get; set; } = ushort.MaxValue - 1;

	/// <inheritdoc/>
	public event SentData? NotifySentData;

	/// <inheritdoc/>
	public event ReceivedData? NotifyReceivedData;

	/// <inheritdoc/>
	public bool Connect()
	{
		try
		{
			Socket.Connect(Endpoint);
			return IsConnected;
		}
		catch
		{
			return false;
		}
	}

	/// <inheritdoc/>
	public bool Disconnect()
	{
		if (IsConnected == false)
			return true;

		try
		{
			Socket.Disconnect(true);
			Socket.Close();

			return true;
		}
		catch
		{
			return false;
		}
		finally
		{
			ConnectionId = 0;
		}
	}

	/// <inheritdoc/>
	public bool SendData(IZkPacket packet) => SendData(packet.ToArray());

	/// <inheritdoc/>
	public bool SendData(byte[] data)
	{
		if (IsConnected == false)
			return false;

		try
		{
			var sent = Socket.Send(data, data.Length, SocketFlags.None);
			NotifySentData?.Invoke(data);

			return sent == data.Length;
		}
		catch
		{
			return false;
		}
	}

	/// <inheritdoc/>
	public byte[] ReceiveData(int length)
	{
		if (IsConnected == false)
			return [];

		try
		{
			var response = new byte[4_096];
			var received = Socket.Receive(response, length + 8, SocketFlags.None);
			var header = response[..8];
			var data = response.Skip(8).Take(received - 8).ToArray();

			if (header.Length == 0 || BitConverter.ToUInt16(header[..2], 0) != TcpPacket.Header1 || BitConverter.ToUInt16(header[2..4], 0) != TcpPacket.Header2)
				throw new Exception();

			NotifyReceivedData?.Invoke(data);

			return data;
		}
		catch
		{
			return [];
		}
	}
}
