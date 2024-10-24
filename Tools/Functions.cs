namespace zkteco_attendance_api;

internal static class Functions
{
	public static byte[] GeneratePassword(int password, int session)
	{
		var encoded = 0;

		for (var i = 0; i < 32; i++)
		{
			if ((password & (1 << i)) != 0)
				encoded = encoded << 1 | 1;
			else
				encoded <<= 1;
		}

		encoded += session;

		var bytes = BitConverter.GetBytes((uint)encoded);

		bytes = BitConverter
			.GetBytes(BitConverter.ToUInt16([(byte)(bytes[2] ^ 'S'), (byte)(bytes[3] ^ 'O')]))
			.Concat(BitConverter.GetBytes(BitConverter.ToUInt16([(byte)(bytes[0] ^ 'Z'), (byte)(bytes[1] ^ 'K')])))
			.ToArray();

		bytes[0] = (byte)(bytes[0] ^ (0xFF & 50));
		bytes[1] = (byte)(bytes[1] ^ (0xFF & 50));
		bytes[2] = 0xFF & 50;
		bytes[3] = (byte)(bytes[3] ^ (0xFF & 50));

		return bytes;
	}
}
