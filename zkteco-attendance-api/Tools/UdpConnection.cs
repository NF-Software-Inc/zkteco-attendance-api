using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace zkteco_attendance_api
{
	/// <summary>
	/// Implementation of <see cref="IConnection"/> to facilitate UDP based connections.
	/// </summary>
	internal class UdpConnection : IConnection
	{
		private const int CommandTimeoutMs = 2_000;
		private readonly UdpClient Client;
		private IPEndPoint Endpoint;

		public UdpConnection(IPAddress ip, int port)
		{
			Endpoint = new IPEndPoint(ip, port);
			Client = new UdpClient();

			Client.Client.SendTimeout = CommandTimeoutMs;
			Client.Client.ReceiveTimeout = CommandTimeoutMs;
		}

		~UdpConnection()
		{
			Disconnect();
			Client.Dispose();
		}

		/// <inheritdoc/>
		public bool IsConnected { get; private set; }

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
				Client.Connect(Endpoint);
				IsConnected = true;

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
				Client.Close();
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				IsConnected = false;
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
				var sent = Client.Send(data, data.Length);
				NotifySentData?.Invoke(data);

				return sent == data.Length;
			}
			catch
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public byte[] ReceivePacket(int length) => ReceiveData(length);

		/// <inheritdoc/>
		public byte[] ReceiveData(int length)
		{
			if (IsConnected == false)
				throw new InvalidOperationException("Connection is not established.");

			try
			{
				var received = Client.Receive(ref Endpoint);
				NotifyReceivedData?.Invoke(received);

				return received;
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

				while (length > 0)
				{
					var received = Client.Receive(ref Endpoint);

					if (received.Length < 8)
						throw new Exception("Invalid UDP packet.");

					stream.Write(received, 8, received.Length - 8);
					length -= (received.Length - 8);
				}

				var data = stream.ToArray();

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
