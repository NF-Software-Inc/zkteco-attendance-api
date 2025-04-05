namespace zkteco_attendance_api
{
	/// <summary>
	/// Defines properties required to connect to a ZKTeco device.
	/// </summary>
	public class ZkDeviceSettings
	{
		public ZkDeviceSettings(string ip)
		{
			Ip = ip;
			Port = 4_370;
			UseTcp = true;
			Password = 0;
		}

		public ZkDeviceSettings(string ip, int port, bool useTcp, int password)
		{
			Ip = ip;
			Port = port;
			UseTcp = useTcp;
			Password = password;
		}

		/// <summary>
		/// The IP address of the ZKTeco device.
		/// </summary>
		public string Ip { get; set; }

		/// <summary>
		/// The port used for communication with the device.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// Specifies whether to use TCP or UDP for communication.
		/// </summary>
		public bool UseTcp { get; set; }

		/// <summary>
		/// The password to authenticate with the device.
		/// </summary>
		public int Password { get; set; }
	}
}
