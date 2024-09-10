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
	public virtual byte[] ToArray()
	{
		if (ResponseId > ushort.MaxValue)
			ResponseId = 1;

		var command = BitConverter.GetBytes(Convert.ToUInt16(Command)).Reverse();
		var checksum = BitConverter.GetBytes(Convert.ToUInt16(0)).Reverse();
		var connection = BitConverter.GetBytes(Convert.ToUInt16(ConnectionId)).Reverse();
		var response = BitConverter.GetBytes(Convert.ToUInt16(ResponseId)).Reverse();

		var array = command
			.Concat(checksum)
			.Concat(connection)
			.Concat(response)
			.Concat(Data)
			.ToArray();

		var current = 0;

		for (var i = 0; i < 8 + Data.Length; i +=2)
		{
			current += array[i];

			if (current > ushort.MaxValue)
				current -= ushort.MaxValue;
		}

		checksum = BitConverter.GetBytes(Convert.ToUInt16(current)).Reverse();
		array[2] = checksum.First();
		array[3] = checksum.Last();

		return array;
	}

	public static IZkPacket ParseHeader(byte[] header)
	{
		var command = BitConverter.ToUInt16(header.Take(2).Reverse().ToArray(), 0);

		return new ZkPacketBase()
		{
			Command = Enum.IsDefined(typeof(Commands), command) ? (Commands)command : Commands.Unknown,
			ConnectionId = BitConverter.ToUInt16(header.Skip(4).Take(2).Reverse().ToArray(), 0),
			ResponseId = BitConverter.ToUInt16(header.Skip(6).Take(2).Reverse().ToArray(), 0)
		};
	}
}
