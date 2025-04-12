using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace zkteco_attendance_api
{
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
		public bool SendPacket(IZkPacket packet) => SendData(packet.ToArray());

		/// <inheritdoc/>
		public bool SendData(byte[] data)
		{
			if (IsConnected == false)
				throw new InvalidOperationException("Connection is not established.");

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
		public byte[] ReceivePacket(int length)
		{
			if (IsConnected == false)
				throw new InvalidOperationException("Connection is not established.");

			try
			{
				var received = ReceiveData(length + 8);

				if (received.Length < 8 || BitConverter.ToUInt16(received, 0) != TcpPacket.Header1 || BitConverter.ToUInt16(received, 2) != TcpPacket.Header2)
					throw new Exception("Invalid TCP header.");

				return received.Skip(8).Take(received.Length - 8).ToArray();
			}
			catch
			{
				return Array.Empty<byte>();
			}
		}

		/// <inheritdoc/>
		public byte[] ReceiveData(int length)
		{
			if (IsConnected == false)
				throw new InvalidOperationException("Connection is not established.");

			try
			{
				var buffer = new byte[length];
				var received = Socket.Receive(buffer, length, SocketFlags.None);
				var data = buffer.Take(received).ToArray();

				NotifyReceivedData?.Invoke(data);
				return data;
			}
			catch
			{
				return Array.Empty<byte>();
			}
		}

		/// <inheritdoc/>
		public byte[] ReceiveBufferedPacket(int length)
		{
			if (IsConnected == false)
				throw new InvalidOperationException("Connection is not established.");

			try
			{
				using var stream = new MemoryStream();
				var first = true;

				length += 16;

				while (length > 0)
				{
					var current = length >= 4_096 ? 4_096 : length;
					var buffer = new byte[4_096];
					var received = Socket.Receive(buffer, current, SocketFlags.None);

					if (first)
					{
						if (received < 8 || BitConverter.ToUInt16(buffer, 0) != TcpPacket.Header1 || BitConverter.ToUInt16(buffer, 2) != TcpPacket.Header2)
							throw new Exception("Invalid TCP header.");

						stream.Write(buffer, 8, received - 8);
						first = false;
					}
					else
					{
						stream.Write(buffer, 0, received);
					}

					length -= received;
				}

				var data = stream.ToArray().Skip(8).ToArray();

				NotifyReceivedData?.Invoke(data);
				return data;
			}
			catch
			{
				return Array.Empty<byte>();
			}
		}
	}
}
