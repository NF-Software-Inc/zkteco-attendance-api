using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using zkteco_attendance_api.Internal;

namespace zkteco_attendance_api
{
	/// <summary>
	/// Main class to communicate with ZKTeco devices.
	/// </summary>
	public class ZkTeco
	{
		private readonly IConnection Connection;
		private readonly ICommand Command;
		private readonly ZkDeviceSettings? Settings;

		/// <summary>
		/// Prepares the class for communications with a ZKTeco device.
		/// </summary>
		/// <param name="ip">The IP address of the ZKTeco device.</param>
		/// <param name="port">The port the ZKTeco device uses for communication.</param>
		/// <param name="useTcp">Spcifies whether to use TCP or UDP communication.</param>
		public ZkTeco(string ip, int port = 4_370, bool useTcp = true)
		{
			var address = IPAddress.Parse(ip);

			if (useTcp)
			{
				var connection = new TcpConnection(address, port);

				Connection = connection;
				Command = new TcpCommand(connection);
			}
			else
			{
				var connection = new UdpConnection(address, port);

				Connection = connection;
				Command = new UdpCommand(connection);
			}

			Connection.NotifyReceivedData += Connection_NotifyReceivedData;
			Connection.NotifySentData += Connection_NotifySentData;
			Command.NotifyCommandError += Command_NotifyCommandError;
		}

		/// <summary>
		/// Prepares the class for communications with a ZKTeco device.
		/// </summary>
		/// <param name="ip">The IP address of the ZKTeco device.</param>
		/// <param name="port">The port the ZKTeco device uses for communication.</param>
		/// <param name="useTcp">Spcifies whether to use TCP or UDP communication.</param>
		public ZkTeco(IPAddress ip, int port = 4_370, bool useTcp = true)
		{
			if (useTcp)
			{
				var connection = new TcpConnection(ip, port);

				Connection = connection;
				Command = new TcpCommand(connection);
			}
			else
			{
				var connection = new UdpConnection(ip, port);

				Connection = connection;
				Command = new UdpCommand(connection);
			}

			Connection.NotifyReceivedData += Connection_NotifyReceivedData;
			Connection.NotifySentData += Connection_NotifySentData;
			Command.NotifyCommandError += Command_NotifyCommandError;
		}

		/// <summary>
		/// Prepares the class for communications with a ZKTeco device.
		/// </summary>
		/// <param name="settings">The object containing device configuration parameters.</param>
		public ZkTeco(ZkDeviceSettings settings)
		{
			var address = IPAddress.Parse(settings.Ip);

			if (settings.UseTcp)
			{
				var connection = new TcpConnection(address, settings.Port);

				Connection = connection;
				Command = new TcpCommand(connection);
			}
			else
			{
				var connection = new UdpConnection(address, settings.Port);

				Connection = connection;
				Command = new UdpCommand(connection);
			}

			Connection.NotifyReceivedData += Connection_NotifyReceivedData;
			Connection.NotifySentData += Connection_NotifySentData;
			Command.NotifyCommandError += Command_NotifyCommandError;

			Settings = settings;
		}

		/// <summary>
		/// Disconnects from the ZKTeco device.
		/// </summary>
		~ZkTeco()
		{
			Connection.NotifyReceivedData -= Connection_NotifyReceivedData;
			Connection.NotifySentData -= Connection_NotifySentData;
			Command.NotifyCommandError -= Command_NotifyCommandError;

			if (Connection.IsConnected)
				Connection.Disconnect();
		}

		/// <inheritdoc cref="IConnection.NotifySentData" />
		public event SentData? NotifySentData;

		/// <inheritdoc cref="IConnection.NotifyReceivedData" />
		public event ReceivedData? NotifyReceivedData;

		/// <inheritdoc cref="ICommand.NotifyCommandError" />
		public event CommandError? NotifyCommandError;

		private void Connection_NotifySentData(byte[] sent) => NotifySentData?.Invoke(sent);
		private void Connection_NotifyReceivedData(byte[] received) => NotifyReceivedData?.Invoke(received);
		private void Command_NotifyCommandError(string message) => NotifyCommandError?.Invoke(message);

		/// <summary>
		/// Indicates whether there is an authenticated connection the the ZKTeco device.
		/// </summary>
		public bool IsConnected
		{
			get => Connection.IsConnected && isConnected;
			private set => isConnected = value;
		}
		private bool isConnected;

		/// <summary>
		/// Opens a connection with the ZKTeco device.
		/// </summary>
		/// <param name="password">The password to authenticate with the ZKTeco device.</param>
		public bool Connect(int password = 0)
		{
			if (IsConnected)
				return true;

			if (Connection.Connect() == false)
				NotifyCommandError?.Invoke("Failed initializing connection to ZKTeco device.");

			var packet = Command.SendCommand(Commands.Connect, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed starting session with ZKTeco device.");
				return false;
			}

			Connection.ConnectionId = packet.ConnectionId;

			if (packet.Command != Commands.Unauthorized)
				return true;

			if (password == 0 && Settings != null)
				password = Settings.Password;

			packet = Command.SendCommand(Commands.Authenticate, Functions.GeneratePassword(password, Connection.ConnectionId), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed requesting authentication with ZKTeco device.");
				return false;
			}

			if (packet.Command != Commands.Success)
				NotifyCommandError?.Invoke("Failed authenticating with ZKTeco device.");

			IsConnected = packet.Command == Commands.Success;
			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Closes the connection with the ZKTeco device.
		/// </summary>
		public bool Disconnect()
		{
			if (IsConnected == false)
				return true;

			var packet = Command.SendCommand(Commands.Disconnect, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed closing session with ZKTeco device.");
				return false;
			}

			if (packet.Command == Commands.Success)
			{
				Connection.ConnectionId = 0;
				Connection.ResponseId = ushort.MaxValue - 1;
			}
			else
			{
				NotifyCommandError?.Invoke("Failed closing session with ZKTeco device.");
			}

			IsConnected = false;
			return packet.Command == Commands.Success && Connection.Disconnect();
		}

		/// <summary>
		/// Returns the extended format of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceExtendedFormat() => GetDeviceConfiguration("~ExtendFmt");

		/// <summary>
		/// Returns the face algorithm of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceFaceVersion() => GetDeviceConfiguration("ZKFaceVersion");

		/// <summary>
		/// Returns the fingerprint algorithm of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceFingerprintVersion() => GetDeviceConfiguration("~ZKFPVersion");

		/// <summary>
		/// Returns the IP address of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceIp() => GetDeviceConfiguration("IPAddress");

		/// <summary>
		/// Returns the gateway IP address of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceGatewayIp() => GetDeviceConfiguration("GATEIPAddress");

		/// <summary>
		/// Returns the MAC address of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceMac() => GetDeviceConfiguration("MAC");

		/// <summary>
		/// Returns the name of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceName() => GetDeviceConfiguration("~DeviceName");

		/// <summary>
		/// Returns the old style firmware version of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceOldFirmwareVersion() => GetDeviceConfiguration("CompatOldFirmware");

		/// <summary>
		/// Returns the platform information of the connected ZKTeco device.
		/// </summary>
		public string? GetDevicePlatform() => GetDeviceConfiguration("~Platform");

		/// <summary>
		/// Returns the serial number of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceSerial() => GetDeviceConfiguration("~SerialNumber");

		/// <summary>
		/// Returns the subnet mask of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceSubnetMask() => GetDeviceConfiguration("NetMask");

		/// <summary>
		/// Returns the user extended format of the connected ZKTeco device.
		/// </summary>
		public string? GetDeviceUserExtendedFormat() => GetDeviceConfiguration("~UserExtFmt");

		private string? GetDeviceConfiguration(string parameter)
		{
			var packet = Command.SendCommand(Commands.ReadConfiguration, parameter, 1_024);

			if (packet == null || packet.Command != Commands.Success)
			{
				NotifyCommandError?.Invoke("Failed getting configuration detail from ZKTeco device.");
				return null;
			}

			return Encoding.UTF8.GetString(packet.Data).Split('=').Last();
		}

		/// <summary>
		/// Enables the connected ZKTeco device and allows user activity.
		/// </summary>
		public bool EnableDevice()
		{
			var packet = Command.SendCommand(Commands.EnableDevice, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed enabling ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Locks the ZKTeco device to prevent user activity.
		/// </summary>
		public bool DisableDevice()
		{
			var packet = Command.SendCommand(Commands.DisableDevice, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed disabling ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Reboots the ZKTeco device.
		/// </summary>
		public bool RestartDevice()
		{
			var packet = Command.SendCommand(Commands.Restart, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed restarting ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Turns off the ZKTeco device.
		/// </summary>
		public bool ShutdownDevice()
		{
			var packet = Command.SendCommand(Commands.PowerOff, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed turning off ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Empties any open buffers on the ZKTeco device.
		/// </summary>
		public bool ClearBuffer()
		{
			var packet = Command.SendCommand(Commands.ClearBuffers, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed clearing buffer on ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Clears any errors on the ZKTeco device.
		/// </summary>
		public bool ClearError()
		{
			var packet1 = Command.SendCommand(Commands.FailedExecute, Array.Empty<byte>(), 1_024);
			var packet2 = Command.SendCommand(Commands.ClearError, Array.Empty<byte>(), 1_024);
			var packet3 = Command.SendCommand(Commands.ClearError, Array.Empty<byte>(), 1_024);
			var packet4 = Command.SendCommand(Commands.ClearError, Array.Empty<byte>(), 1_024);

			if (packet1 == null)
			{
				NotifyCommandError?.Invoke("Failed clearing error on ZKTeco device 1.");
				return false;
			}
			else if (packet2 == null || packet3 == null || packet4 == null)
			{
				NotifyCommandError?.Invoke("Failed clearing error on ZKTeco device 2.");
				return false;
			}

			return packet1.Command == Commands.Success && packet2.Command == Commands.Success && packet3.Command == Commands.Success && packet4.Command == Commands.Success;
		}

		/// <summary>
		/// Returns the current time on the ZKTeco device.
		/// </summary>
		public DateTime? GetTime()
		{
			var packet = Command.SendCommand(Commands.GetTime, Array.Empty<byte>(), 1_024);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed getting time from ZKTeco device.");
				return null;
			}

			var time = BitConverter.ToInt32(packet.Data);
			return ConvertDate(time);
		}

		/// <summary>
		/// Sets the current time on the ZKTeco device to the current time on the local device.
		/// </summary>
		public bool SetTime()
		{
			var packet = Command.SendCommand(Commands.SetTime, BitConverter.GetBytes(ConvertDate(DateTime.Now)), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed setting time on ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Sets the current time on the ZKTeco device.
		/// </summary>
		/// <param name="time">The time to set on the ZKTeco device.</param>
		public bool SetTime(DateTime time)
		{
			var packet = Command.SendCommand(Commands.SetTime, BitConverter.GetBytes(ConvertDate(time)), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed setting time on ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Returns the currently active firmware edition on the ZKTeco device.
		/// </summary>
		public string? GetFirmwareVersion()
		{
			var packet = Command.SendCommand(Commands.FirmwareVersion, Array.Empty<byte>(), 1_024);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed getting firmware version from ZKTeco device.");
				return null;
			}

			return Encoding.UTF8.GetString(packet.Data);
		}

		/// <summary>
		/// Returns the current counts of stored items on the ZKTeco device.
		/// </summary>
		public RecordCounts? GetStorageDetails()
		{
			var packet = Command.SendCommand(Commands.CheckStorage, Array.Empty<byte>(), 1_024);

			if (packet == null || packet.Command != Commands.Success || packet.Data.Length < 80)
			{
				NotifyCommandError?.Invoke("Failed getting storage details from ZKTeco device.");
				return null;
			}

			var data = packet.Data.Partition(4, false).Select(x => BitConverter.ToInt32(x, 0)).ToList();
			return new RecordCounts() { Users = data[4], AvailableUsers = data[18], MaximumUsers = data[15], Records = data[8], AvailableRecords = data[19], MaximumRecords = data[16], Fingers = data[6], AvailableFingers = data[17], MaximumFingers = data[14] };
		}

		/// <summary>
		/// Returns a list of users that exist on the ZKTeco device.
		/// </summary>
		public List<ZkTecoUser>? GetUsers()
		{
			var users = new List<ZkTecoUser>();
			var counts = GetStorageDetails();

			if (counts != null && counts.Users == 0)
				return users;

			var data = BitConverter.GetBytes(5).Concat(BitConverter.GetBytes(0)).ToArray();
			var packet = Command.SendBufferedCommand(Commands.ReadUsers, data);

			if (packet == null || packet.Command != Commands.Success)
			{
				NotifyCommandError?.Invoke("Failed preparing read of users on ZKTeco device.");
				return null;
			}

			var size = counts != null ? (packet.Data.Length - 4) / counts.Users : 72;

			if (size != 72)
			{
				NotifyCommandError?.Invoke($"User size not supported. Supported size is 72, actual is {size}.");
				return null;
			}

			foreach (var item in packet.Data.Skip(4).Partition(size, false))
			{
				var index = BitConverter.ToUInt16(item, 0);
				var privilege = Enum.IsDefined(typeof(Privilege), (int)item[2]) ? (Privilege)(int)item[2] : Privilege.Default;
				var password = Encoding.UTF8.GetString(item[3..11]);
				var name = Encoding.UTF8.GetString(item[11..35]).Split('\0').First();
				var card = BitConverter.ToInt32(item, 35);
				var group = Encoding.UTF8.GetString(item[40..47]).Split('\0').First();
				var id = Encoding.UTF8.GetString(item[48..]).Split('\0').First();

				users.Add(new ZkTecoUser() { Index = index, Name = name, Password = password, Privilege = privilege, Group = group, UserId = id, Card = card });
			}

			return users;
		}

		/// <summary>
		/// Adds a new user to the ZKTeco device with the provided values.
		/// </summary>
		/// <param name="name">The full name of the user to add.</param>
		/// <param name="userId">The unique identifier to assign to the user.</param>
		public ZkTecoUser? CreateUser(string name, string userId)
		{
			var users = GetUsers();

			var max = users != null && users.Count > 0 ?  users.Max(x => x.Index) : 0;
			var user = new ZkTecoUser() { Index = max + 1, Name = name[..24], Password = null, Privilege = Privilege.Default, Group = null, UserId = userId[..24], Card = 0 };

			var nameBytes = new byte[24];
			var bytes = Encoding.UTF8.GetBytes(user.Name);

			for (var i = 0; i < nameBytes.Length && i < bytes.Length; i++)
				nameBytes[i] = bytes[i];

			var id = new byte[24];
			bytes = Encoding.UTF8.GetBytes(user.UserId);

			for (var i = 0; i < id.Length && i < bytes.Length; i++)
				id[i] = bytes[i];

			var data = BitConverter.GetBytes((ushort)user.Index)
				.Append((byte)(int)user.Privilege)
				.Concat(new byte[8])
				.Concat(nameBytes)
				.Concat(BitConverter.GetBytes(user.Card))
				.Append((byte)0x00)
				.Concat(new byte[7])
				.Append((byte)0x00)
				.Concat(id)
				.ToArray();

			var packet = Command.SendCommand(Commands.CreateUser, data, 1_024);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed creating user on ZKTeco device.");
				return null;
			}

			_ = RefreshData();
			return user;
		}

		/// <summary>
		/// Adds a new user to the ZKTeco device with the provided values.
		/// </summary>
		/// <param name="user">The details of the user to create.</param>
		public bool CreateUser(ZkTecoUser user)
		{
			var password = new byte[8];
			var bytes = Encoding.UTF8.GetBytes(user.Password ?? string.Empty);

			for (var i = 0; i < password.Length && i < bytes.Length; i++)
				password[i] = bytes[i];

			var nameBytes = new byte[24];
			bytes = Encoding.UTF8.GetBytes(user.Name);

			for (var i = 0; i < nameBytes.Length && i < bytes.Length; i++)
				nameBytes[i] = bytes[i];

			var group = new byte[7];
			bytes = Encoding.UTF8.GetBytes(user.Group ?? string.Empty);

			for (var i = 0; i < group.Length && i < bytes.Length; i++)
				group[i] = bytes[i];

			var id = new byte[24];
			bytes = Encoding.UTF8.GetBytes(user.UserId);

			for (var i = 0; i < id.Length && i < bytes.Length; i++)
				id[i] = bytes[i];

			var data = BitConverter.GetBytes((ushort)user.Index)
				.Append((byte)(int)user.Privilege)
				.Concat(password)
				.Concat(nameBytes)
				.Concat(BitConverter.GetBytes(user.Card))
				.Append((byte)0x00)
				.Concat(group)
				.Append((byte)0x00)
				.Concat(id)
				.ToArray();

			var packet = Command.SendCommand(Commands.CreateUser, data, 1_024);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed creating user on ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success && RefreshData();
		}

		/// <summary>
		/// Updates an existing user on the ZKTeco device with the provided values.
		/// </summary>
		/// <param name="user"> The details of the user to update.</param>
		/// <remarks>
		/// Alias of CreateUser, as the ZKTeco device does not have a specific command to update users.
		/// </remarks>
		public bool UpdateUser(ZkTecoUser user) => CreateUser(user);

		/// <summary>
		/// Deletes all attendance records on the ZKTeco device.
		/// </summary>
		/// <param name="index">The device specific uid of the user to delete.</param>
		public bool DeleteUser(int index)
		{
			var packet = Command.SendCommand(Commands.DeleteUser, BitConverter.GetBytes((ushort)index), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed deleting user from ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success && RefreshData();
		}

		/// <summary>
		/// Deletes all attendance records on the ZKTeco device.
		/// </summary>
		/// <param name="user">The user to delete.</param>
		public bool DeleteUser(ZkTecoUser user)
		{
			var packet = Command.SendCommand(Commands.DeleteUser, BitConverter.GetBytes((ushort)user.Index), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed deleting user from ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success && RefreshData();
		}

		/// <summary>
		/// Returns all attendance records on the ZKTeco device.
		/// </summary>
		public List<ZkTecoAttendance>? GetAttendance()
		{
			var attendances = new List<ZkTecoAttendance>();
			var counts = GetStorageDetails();

			if (counts != null && counts.Records == 0)
				return attendances;

			var data = BitConverter.GetBytes(0).Concat(BitConverter.GetBytes(0)).ToArray();
			var packet = Command.SendBufferedCommand(Commands.ReadAttendance, data);

			if (packet == null || packet.Command != Commands.Success)
			{
				NotifyCommandError?.Invoke("Failed preparing reading attendance data on ZKTeco device.");
				return null;
			}

			var size = counts != null ? (packet.Data.Length - 4) / counts.Records : 40;

			if (size < 40)
			{
				NotifyCommandError?.Invoke($"Attendance record size not supported. Supported size is 40 and larger, actual is {size}.");
				return null;
			}

			foreach (var item in packet.Data.Skip(4).Partition(size, false))
			{
				var index = BitConverter.ToUInt16(item, 0);
				var id = Encoding.UTF8.GetString(item[2..26]).Split('\0').First();
				var status = item[26];
				var time = BitConverter.ToInt32(item, 27);
				var punch = item[31];

				attendances.Add(new ZkTecoAttendance() { Index = index, Timestamp = ConvertDate(time), UserId = id, Status = status, Punch = punch });
			}

			return attendances;
		}

		/// <summary>
		/// Deletes all attendance records on the ZKTeco device.
		/// </summary>
		public bool ClearAttendance()
		{
			var packet = Command.SendCommand(Commands.DeleteAttendance, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed clearing attendance data on ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Sets the display text on the ZKTeco device.
		/// </summary>
		/// <param name="text">The text to display on the ZKTeco device.</param>
		/// <param name="line">The line to display the text on.</param>
		public bool SetDisplayText(string text, short line = 1)
		{
			var data = BitConverter.GetBytes(line)
				.Append((byte)0x00)
				.Concat(Encoding.UTF8.GetBytes(text))
				.ToArray();

			var packet = Command.SendCommand(Commands.SetDisplay, data, ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed setting display text on ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Clears the display text on the ZKTeco device.
		/// </summary>
		public bool ClearDisplayText()
		{
			var packet = Command.SendCommand(Commands.ClearDisplay, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed clearing display text on ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		/// <summary>
		/// Refreshes the data on the ZKTeco device.
		/// </summary>
		/// <remarks>
		/// This is required to be able to read the attendance records after adding or deleting users. This will be automatically called when creating or deleting users.
		/// </remarks>
		public bool RefreshData()
		{
			var packet = Command.SendCommand(Commands.RefreshData, Array.Empty<byte>(), ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("Failed refreshing data on ZKTeco device.");
				return false;
			}

			return packet.Command == Commands.Success;
		}

		private int ConvertDate(DateTime value)
		{
			var year = value.Year - 2_000;
			var month = value.Month - 1;
			var day = value.Day - 1;
			var hours = value.Hour;
			var minutes = value.Minute;
			var seconds = value.Second;

			return (int)(year * 12 * 31 * 24 * 60 * 60 + month * 31 * 24 * 60 * 60 + day * 24 * 60 * 60 + hours * 60 * 60 + minutes * 60 + seconds);
		}

		private DateTime ConvertDate(int value)
		{
			var seconds = value % 60;
			value = (int)Math.Floor(value / 60.0M);

			var minutes = value % 60;
			value = (int)Math.Floor(value / 60.0M);

			var hours = value % 24;
			value = (int)Math.Floor(value / 24.0M);

			var day = (value % 31) + 1;
			value = (int)Math.Floor(value / 31.0M);

			var month = (value % 12) + 1;
			value = (int)Math.Floor(value / 12.0M);

			var year = value + 2_000;

			return new DateTime(year, month, day, hours, minutes, seconds);
		}
	}
}
