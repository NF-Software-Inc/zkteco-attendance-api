namespace zkteco_attendance_api;

/// <summary>
/// Facilitates building TCP messages to send to ZKTeco devices.
/// </summary>
internal class TcpPacket : ZkPacketBase, IZkPacket
{
	/// <summary>
	/// Standard ZKTeco header bits 1.
	/// </summary>
	internal const ushort Header1 = 20560;

	/// <summary>
	/// Standard ZKTeco header bits 2.
	/// </summary>
	internal const ushort Header2 = 32130;

	/// <inheritdoc/>
	public override byte[] ToArray()
	{
		var header1 = BitConverter.GetBytes(Convert.ToUInt16(Header1)).Reverse();
		var header2 = BitConverter.GetBytes(Convert.ToUInt16(Header2)).Reverse();
		var length = BitConverter.GetBytes(Convert.ToUInt32(Data.Length)).Reverse();

		return header1
			.Concat(header2)
			.Concat(length)
			.Concat(base.ToArray())
			.ToArray();
	}
}
