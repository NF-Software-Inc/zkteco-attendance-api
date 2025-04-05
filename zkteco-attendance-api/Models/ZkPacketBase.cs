using System;
using System.Linq;
using zkteco_attendance_api.Internal;

namespace zkteco_attendance_api
{
	/// <summary>
	/// Base class to construct messages for communicating with ZKTeco devices.
	/// </summary>
	internal class ZkPacketBase : IZkPacket
	{
		internal const int DefaultHeaderLength = 8;

		/// <inheritdoc/>
		public Commands Command { get; set; }

		/// <inheritdoc/>
		public int ConnectionId { get; set; }

		/// <inheritdoc/>
		public int ResponseId { get; set; }

		/// <inheritdoc/>
		public byte[] Data { get; set; } = Array.Empty<byte>();

		/// <inheritdoc/>
		public virtual byte[] ToArray()
		{
			var current = 0;
			var items = new int[] { (int)Command, ConnectionId, ResponseId };
			var data = Data.Partition(2, false).Select(x => (int)BitConverter.ToUInt16(x, 0));

			foreach (var item in items.Concat(data))
			{
				current += item;

				if (current > ushort.MaxValue)
					current -= ushort.MaxValue;
			}

			current = ~current;

			if (current < 0)
				current += ushort.MaxValue;

			ResponseId++;

			if (ResponseId >= ushort.MaxValue)
				ResponseId -= ushort.MaxValue;

			var command = BitConverter.GetBytes(Convert.ToUInt16(Command));
			var checksum = BitConverter.GetBytes(Convert.ToUInt16(current));
			var connection = BitConverter.GetBytes(Convert.ToUInt16(ConnectionId));
			var response = BitConverter.GetBytes(Convert.ToUInt16(ResponseId));

			var array = command
				.Concat(checksum)
				.Concat(connection)
				.Concat(response)
				.Concat(Data)
				.ToArray();

			return array;
		}

		/// <summary>
		/// Parses the provided byte array into a packet.
		/// </summary>
		/// <param name="header">The data to parse.</param>
		public static IZkPacket ParseHeader(byte[] header)
		{
			var command = (int)BitConverter.ToUInt16(header, 0);

			return new ZkPacketBase()
			{
				Command = Enum.IsDefined(typeof(Commands), command) ? (Commands)command : Commands.Unknown,
				ConnectionId = BitConverter.ToUInt16(header, 4),
				ResponseId = BitConverter.ToUInt16(header, 6)
			};
		}
	}
}
