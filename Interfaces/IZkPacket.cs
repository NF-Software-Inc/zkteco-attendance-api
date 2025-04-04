namespace zkteco_attendance_api
{
	/// <summary>
	/// Defines properties and methods required for a ZKTeco command packet.
	/// </summary>
	internal interface IZkPacket
	{
		/// <summary>
		/// The operation to execute on the ZKTeco device.
		/// </summary>
		Commands Command { get; set; }

		/// <summary>
		/// The session to connect to on the ZKTeco device.
		/// </summary>
		int ConnectionId { get; set; }

		/// <summary>
		/// The identifier for the response from the device.
		/// </summary>
		int ResponseId { get; set; }

		/// <summary>
		/// The information to send to the ZKTeco device.
		/// </summary>
		byte[] Data { get; set; }

		/// <summary>
		/// Returns the packet as a byte array ready for sending to the ZKTeco device.
		/// </summary>
		byte[] ToArray();
	}
}
