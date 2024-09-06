namespace zkteco_attendance_api;

/// <summary>
/// Base class to construct messages for communicating with ZKTeco devices.
/// </summary>
internal class ZkPacketBase : IZkPacket
{
	/// <inheritdoc/>
	public Commands Command { get; init; }

	/// <inheritdoc/>
	public int ConnectionId { get; init; }

	/// <inheritdoc/>
	public int ResponseId { get; set; }

	/// <inheritdoc/>
	public byte[] Data { get; set; } = [];

	/// <inheritdoc/>
	public byte[] ToArray()
	{
		throw new NotImplementedException();
	}
}
