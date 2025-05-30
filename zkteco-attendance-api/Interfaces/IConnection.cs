﻿using System;

namespace zkteco_attendance_api
{
	/// <summary>
	/// Defines methods and properties required to communicate with a ZKTeco device.
	/// </summary>
	internal interface IConnection
	{
		/// <summary>
		/// Specifies whether there is an active connection to the ZKTeco device.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// The identifier for the current connection to the ZKTeco device.
		/// </summary>
		int ConnectionId { get; set; }

		/// <summary>
		/// The identifier for the current response from the device.
		/// </summary>
		int ResponseId { get; set; }

		/// <summary>
		/// Notifies data was sent to the ZKTeco device and returns the bytes that were sent.
		/// </summary>
		event SentData? NotifySentData;

		/// <summary>
		/// Notifies data was received from the ZKTeco device and returns the bytes that were received.
		/// </summary>
		event ReceivedData? NotifyReceivedData;

		/// <summary>
		/// Opens a connection with the ZKTeco device.
		/// </summary>
		bool Connect();

		/// <summary>
		/// Closes the connection with the ZKTeco device.
		/// </summary>
		bool Disconnect();

		/// <summary>
		/// Sends the provided data to the ZKTeco device.
		/// </summary>
		/// <param name="data">The data to send.</param>
		/// <exception cref="InvalidOperationException">Thrown when the connection has not been established.</exception>
		bool SendData(byte[] data);

		/// <summary>
		/// Receives the specified amount of data from the ZKTeco device.
		/// </summary>
		/// <param name="length">The amount of data to receive.</param>
		/// <exception cref="InvalidOperationException">Thrown when the connection has not been established.</exception>
		byte[] ReceiveData(int length);

		/// <summary>
		/// Sends the provided packet to the ZKTeco device.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		/// <exception cref="InvalidOperationException">Thrown when the connection has not been established.</exception>
		bool SendPacket(IZkPacket packet);

		/// <summary>
		/// Receives a packet with the specified length from the ZKTeco device.
		/// </summary>
		/// <param name="length">The amount of data to receive.</param>
		/// <exception cref="InvalidOperationException">Thrown when the connection has not been established.</exception>
		byte[] ReceivePacket(int length);

		/// <summary>
		/// Receives a packet with the specified length in a stream and returns it as an array.
		/// </summary>
		/// <param name="length">The amount of data to receive.</param>
		/// <exception cref="InvalidOperationException">Thrown when the connection has not been established.</exception>
		byte[] ReceiveBufferedPacket(int length);
	}

	/// <param name="sent">The data that was sent to the ZKTeco device.</param>
	public delegate void SentData(byte[] sent);

	/// <param name="received">The data that was received from the PLC</param>
	public delegate void ReceivedData(byte[] received);
}
