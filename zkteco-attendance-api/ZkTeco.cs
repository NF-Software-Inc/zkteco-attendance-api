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
		/// The time in milliseconds to wait for the device to answer a command before giving up, 2000 by default.
		/// </summary>
		/// <remarks>
		/// Some devices take longer than the default to finish certain commands. A command that times out returns a failure even if the device went on to complete it.
		/// </remarks>
		public int CommandTimeout
		{
			get => Connection.Timeout;
			set => Connection.Timeout = value;
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
			{
				IsConnected = true;
				return true;
			}

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
			var counts = new RecordCounts() { Users = data[4], AvailableUsers = data[18], MaximumUsers = data[15], Records = data[8], AvailableRecords = data[19], MaximumRecords = data[16], Fingers = data[6], AvailableFingers = data[17], MaximumFingers = data[14] };

			// Devices with newer firmware append the face counts.
			if (data.Count > 22)
			{
				counts.Faces = data[20];
				counts.MaximumFaces = data[22];
			}

			return counts;
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
		/// Returns all fingerprint templates stored on the ZKTeco device.
		/// </summary>
		public List<ZkTecoFingerprint>? GetFingerprints()
		{
			var fingerprints = new List<ZkTecoFingerprint>();
			var counts = GetStorageDetails();

			if (counts != null && counts.Fingers == 0)
				return fingerprints;

			// Function 2 requests the fingerprint template table.
			var data = BitConverter.GetBytes(2).Concat(BitConverter.GetBytes(0)).ToArray();
			var packet = Command.SendBufferedCommand(Commands.ReadDatabase, data);

			if (packet == null || packet.Command != Commands.Success)
			{
				NotifyCommandError?.Invoke("Failed preparing read of fingerprints on ZKTeco device.");
				return null;
			}

			if (packet.Data.Length < 4)
				return fingerprints;

			var total = BitConverter.ToInt32(packet.Data, 0);
			var end = Math.Min(packet.Data.Length, 4 + Math.Max(total, 0));
			var offset = 4;

			while (offset + 6 <= end)
			{
				var size = (int)BitConverter.ToUInt16(packet.Data, offset);

				if (size < 6 || offset + size > end)
				{
					NotifyCommandError?.Invoke("Invalid fingerprint record received from ZKTeco device.");
					return null;
				}

				var userIndex = (int)BitConverter.ToUInt16(packet.Data, offset + 2);
				var fingerIndex = (int)packet.Data[offset + 4];
				var flag = Enum.IsDefined(typeof(FingerprintFlag), (int)packet.Data[offset + 5]) ? (FingerprintFlag)(int)packet.Data[offset + 5] : FingerprintFlag.Invalid;
				var template = packet.Data[(offset + 6)..(offset + size)];

				fingerprints.Add(new ZkTecoFingerprint(userIndex, fingerIndex, template, flag));
				offset += size;
			}

			return fingerprints;
		}

		/// <summary>
		/// Returns all fingerprint templates stored on the ZKTeco device for the specified user.
		/// </summary>
		/// <param name="user">The user to return fingerprints for.</param>
		public List<ZkTecoFingerprint>? GetFingerprints(ZkTecoUser user) => GetFingerprints(user.Index);

		/// <summary>
		/// Returns all fingerprint templates stored on the ZKTeco device for the specified user.
		/// </summary>
		/// <param name="userIndex">The device specific uid of the user to return fingerprints for.</param>
		/// <remarks>
		/// Only the requested user's fingers are read from the device (up to 10 small requests), the other users' templates are not transferred.
		/// See <see cref="GetFingerprint(int, int)"/> for details on how this falls back for devices that do not support it.
		/// </remarks>
		public List<ZkTecoFingerprint>? GetFingerprints(int userIndex)
		{
			var tried = false;

			if (CanReadFingerprintsDirectly)
			{
				tried = true;
				var found = new List<ZkTecoFingerprint>();
				var answered = true;

				for (var finger = 0; finger <= ZkTecoFingerprint.MaximumFingerIndex && answered; finger++)
				{
					var fingerprint = ReadFingerprintDirectly(userIndex, finger, out answered);

					if (fingerprint != null)
						found.Add(fingerprint);
				}

				if (answered)
				{
					if (found.Count > 0)
					{
						DirectFingerprintRead = true;
						return found;
					}

					if (DirectFingerprintRead == true)
						return found;
				}
			}

			// Support is unknown and nothing was found, so confirm with a full read.
			var result = GetFingerprints()?.Where(x => x.UserIndex == userIndex).ToList();

			if (tried && result != null && result.Count > 0)
				DirectFingerprintRead = false;

			return result;
		}

		/// <summary>
		/// Returns a single fingerprint template for the specified user, or null if it does not exist.
		/// </summary>
		/// <param name="userIndex">The device specific uid of the user that owns the fingerprint.</param>
		/// <param name="fingerIndex">The index of the finger, from 0 to 9.</param>
		/// <remarks>
		/// Only the requested template is read from the device. This is the request the manufacturer's SDK uses, available over TCP.
		/// If the device does not answer it (UDP connection or unsupported firmware), the templates of all users are read instead and the result is filtered,
		/// which is slower. Once the device has answered the direct read successfully, a missing finger is reported as null without any further reads.
		/// </remarks>
		public ZkTecoFingerprint? GetFingerprint(int userIndex, int fingerIndex)
		{
			var tried = false;

			if (CanReadFingerprintsDirectly)
			{
				tried = true;
				var fingerprint = ReadFingerprintDirectly(userIndex, fingerIndex, out var answered);

				if (fingerprint != null)
				{
					DirectFingerprintRead = true;
					return fingerprint;
				}

				if (answered && DirectFingerprintRead == true)
					return null;
			}

			// Support is unknown and nothing was found, so confirm with a full read.
			var result = GetFingerprints()?.FirstOrDefault(x => x.UserIndex == userIndex && x.FingerIndex == fingerIndex);

			if (tried && result != null)
				DirectFingerprintRead = false;

			return result;
		}

		/// <summary>
		/// Specifies whether <see cref="GetFingerprint(int, int)"/> and <see cref="GetFingerprints(int)"/> may read a single user's templates directly from the device.
		/// </summary>
		/// <remarks>
		/// Null (the default) means unknown: the first read tries the direct method and, if the device ignores it, remembers that and switches to reading all templates.
		/// That first attempt costs one command timeout, so if you know your device doesn't support it set this to false to skip it.
		/// </remarks>
		public bool? DirectFingerprintRead { get; set; }

		private bool CanReadFingerprintsDirectly => DirectFingerprintRead != false && Connection is TcpConnection;

		private ZkTecoFingerprint? ReadFingerprintDirectly(int userIndex, int fingerIndex, out bool answered)
		{
			var data = BitConverter.GetBytes((ushort)userIndex).Append((byte)fingerIndex).ToArray();
			var packet = Command.SendCommandForData(Commands.ReadFingerprint, data);

			// No reply at all means the device does not understand the command, so stop using it.
			answered = packet != null;

			if (packet == null)
			{
				// A device that has answered before is only having a transient problem, don't give up on it.
				if (DirectFingerprintRead != true)
					DirectFingerprintRead = false;

				return null;
			}

			// The data is the bare template, the device replies with an error when the finger has none.
			if (packet.Command != Commands.Success || packet.Data.Length == 0)
				return null;

			return new ZkTecoFingerprint(userIndex, fingerIndex, packet.Data);
		}

		/// <summary>
		/// Writes a fingerprint template to the ZKTeco device.
		/// </summary>
		/// <param name="user">The user that owns the fingerprint.</param>
		/// <param name="fingerprint">The fingerprint template to write.</param>
		/// <remarks>See <see cref="SetFingerprints(ZkTecoUser, IEnumerable{ZkTecoFingerprint}, bool)"/> for important details about how the user record is handled.</remarks>
		/// <param name="refreshData">See <see cref="SetFingerprints(ZkTecoUser, IEnumerable{ZkTecoFingerprint}, bool)"/>.</param>
		public bool SetFingerprint(ZkTecoUser user, ZkTecoFingerprint fingerprint, bool refreshData = true) => SetFingerprints(user, new[] { fingerprint }, refreshData);

		/// <summary>
		/// Writes one or more fingerprint templates for an existing user to the ZKTeco device.
		/// </summary>
		/// <param name="user">The user that owns the fingerprints. The user must already exist on the device, see <see cref="CreateUser(ZkTecoUser)"/>.</param>
		/// <param name="fingerprints">The fingerprint templates to write.</param>
		/// <param name="refreshData">
		/// Whether to ask the device to refresh its data and re-enable it after writing. Leave true for a single user. When writing many users in a row,
		/// pass false for each, then call <see cref="RefreshData"/> and <see cref="EnableDevice"/> once at the end.
		/// </param>
		/// <remarks>
		/// Only the templates are written, the user record is not changed and only <see cref="ZkTecoUser.Index"/> is used from <paramref name="user"/>.
		/// The device is disabled while writing. It is enabled again afterwards when <paramref name="refreshData"/> is true or when writing fails.
		/// Writing a finger that already has a template is expected to replace it.
		/// </remarks>
		public bool SetFingerprints(ZkTecoUser user, IEnumerable<ZkTecoFingerprint> fingerprints, bool refreshData = true)
		{
			var items = fingerprints.ToList();

			if (items.Count == 0)
			{
				NotifyCommandError?.Invoke("No fingerprints were provided to write to ZKTeco device.");
				return false;
			}

			if (items.Any(x => x.Template == null || x.Template.Length == 0 || x.Template.Length > ushort.MaxValue) || items.Any(x => x.FingerIndex < 0 || x.FingerIndex > ZkTecoFingerprint.MaximumFingerIndex) || items.Select(x => x.FingerIndex).Distinct().Count() != items.Count)
			{
				NotifyCommandError?.Invoke("Invalid fingerprint provided to write to ZKTeco device. Templates must not be empty, and finger indexes must be unique and between 0 and 9.");
				return false;
			}

			if (DisableDevice() == false)
				return false;

			var success = false;

			try
			{
				success = items.All(x => WriteFingerprint(user.Index, x)) && (refreshData == false || RefreshData());
				return success;
			}
			finally
			{
				if (refreshData || success == false)
					EnableDevice();
			}
		}

		/// <summary>
		/// Deletes a single fingerprint template from the ZKTeco device.
		/// </summary>
		/// <param name="user">The user that owns the fingerprint.</param>
		/// <param name="fingerIndex">The index of the finger to delete, from 0 to 9.</param>
		public bool DeleteFingerprint(ZkTecoUser user, int fingerIndex)
		{
			var data = PadBytes(user.UserId, 24).Append((byte)fingerIndex).ToArray();
			var packet = Command.SendCommand(Commands.DeleteFingerprint, data, ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("No response from ZKTeco device while deleting fingerprint. Some devices stay silent when the user has no fingerprint at that finger index, check with GetFingerprints(user) first.");
				return false;
			}

			if (packet.Command != Commands.Success)
			{
				NotifyCommandError?.Invoke($"ZKTeco device refused to delete fingerprint ({packet.Command}). The user may not have a fingerprint at that index.");
				return false;
			}

			return RefreshData();
		}

		/// <summary>
		/// Deletes a single fingerprint template using the request that some other open-source libraries use (user index and finger index, rather than the user's id).
		/// </summary>
		/// <param name="userIndex">The device specific uid of the user that owns the fingerprint.</param>
		/// <param name="fingerIndex">The index of the finger to delete, from 0 to 9.</param>
		/// <remarks>
		/// Only use this if <see cref="DeleteFingerprint(ZkTecoUser, int)"/> does not work on your device. The manufacturer's SDK uses this same command to delete
		/// all of a user's fingerprints, so a device may ignore the finger index and delete every template the user has. Back up the user's templates first.
		/// </remarks>
		public bool DeleteFingerprintLegacy(int userIndex, int fingerIndex)
		{
			var data = BitConverter.GetBytes((ushort)userIndex).Append((byte)fingerIndex).ToArray();
			var packet = Command.SendCommand(Commands.DeleteUserFingerprints, data, ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("No response from ZKTeco device while deleting fingerprint. Some devices stay silent when the user has no fingerprint at that finger index, check with GetFingerprints(user) first.");
				return false;
			}

			if (packet.Command != Commands.Success)
			{
				NotifyCommandError?.Invoke($"ZKTeco device refused to delete fingerprint ({packet.Command}).");
				return false;
			}

			return RefreshData();
		}

		/// <summary>
		/// Deletes a single fingerprint template from the ZKTeco device.
		/// </summary>
		/// <param name="userIndex">The device specific uid of the user that owns the fingerprint.</param>
		/// <param name="fingerIndex">The index of the finger to delete, from 0 to 9.</param>
		/// <remarks>
		/// The device identifies the user by their <see cref="ZkTecoUser.UserId"/> for this command, so all users are read to find it.
		/// Use <see cref="DeleteFingerprint(ZkTecoUser, int)"/> when you already have the user.
		/// </remarks>
		public bool DeleteFingerprint(int userIndex, int fingerIndex)
		{
			var user = GetUsers()?.FirstOrDefault(x => x.Index == userIndex);

			if (user == null)
			{
				NotifyCommandError?.Invoke("User to delete fingerprint for was not found on ZKTeco device.");
				return false;
			}

			return DeleteFingerprint(user, fingerIndex);
		}

		/// <summary>
		/// Deletes a single fingerprint template from the ZKTeco device.
		/// </summary>
		/// <param name="fingerprint">The fingerprint to delete.</param>
		public bool DeleteFingerprint(ZkTecoFingerprint fingerprint) => DeleteFingerprint(fingerprint.UserIndex, fingerprint.FingerIndex);

		/// <summary>
		/// Deletes all fingerprint templates for the specified user from the ZKTeco device.
		/// </summary>
		/// <param name="userIndex">The device specific uid of the user to delete fingerprints for.</param>
		public bool DeleteFingerprints(int userIndex)
		{
			var data = BitConverter.GetBytes((ushort)userIndex).Append((byte)0).ToArray();
			var packet = Command.SendCommand(Commands.DeleteUserFingerprints, data, ZkPacketBase.DefaultHeaderLength);

			if (packet == null)
			{
				NotifyCommandError?.Invoke("No response from ZKTeco device while deleting fingerprints.");
				return false;
			}

			if (packet.Command != Commands.Success)
			{
				NotifyCommandError?.Invoke($"ZKTeco device refused to delete fingerprints ({packet.Command}).");
				return false;
			}

			return RefreshData();
		}

		/// <summary>
		/// Deletes all fingerprint templates for the specified user from the ZKTeco device.
		/// </summary>
		/// <param name="user">The user to delete fingerprints for.</param>
		public bool DeleteFingerprints(ZkTecoUser user) => DeleteFingerprints(user.Index);

		/// <summary>
		/// Uploads a single template to the device buffer and transfers it to the user, following the same steps as the manufacturer's SDK.
		/// </summary>
		private bool WriteFingerprint(int userIndex, ZkTecoFingerprint fingerprint)
		{
			var size = BitConverter.GetBytes((ushort)fingerprint.Template.Length);
			var write = BitConverter.GetBytes((ushort)userIndex).Append((byte)fingerprint.FingerIndex).Append((byte)(int)fingerprint.Flag).Concat(size).ToArray();

			return SendFingerprintStep(Commands.ClearBuffers, Array.Empty<byte>(), "clearing the buffer")
				&& SendFingerprintStep(Commands.PrepareData, size.Concat(new byte[2]).ToArray(), "preparing to receive the template")
				&& SendFingerprintStep(Commands.SendData, fingerprint.Template, "receiving the template")
				&& SendFingerprintStep(Commands.ChecksumBuffer, Array.Empty<byte>(), "checking the template")
				&& SendFingerprintStep(Commands.WriteFingerprint, write, "saving the template")
				&& SendFingerprintStep(Commands.ClearBuffers, Array.Empty<byte>(), "releasing the buffer");
		}

		private bool SendFingerprintStep(Commands command, byte[] data, string step)
		{
			var packet = Command.SendCommand(command, data, 1_024);

			if (packet == null)
			{
				NotifyCommandError?.Invoke($"No response from ZKTeco device while {step}.");
				return false;
			}

			if (packet.Command != Commands.Success)
			{
				NotifyCommandError?.Invoke($"ZKTeco device failed {step} ({packet.Command}).");
				return false;
			}

			return true;
		}

		private static byte[] PadBytes(string? value, int length)
		{
			var result = new byte[length];
			var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);

			Array.Copy(bytes, result, Math.Min(bytes.Length, length));
			return result;
		}

		/// <summary>
		/// Returns the face templates stored on the ZKTeco device for the specified user.
		/// </summary>
		/// <param name="userIndex">The device specific uid of the user to return faces for.</param>
		/// <returns>The user's faces (empty if the device reports none for the user), or null if the device did not answer or returned data in an unexpected form.</returns>
		/// <remarks>
		/// EXPERIMENTAL. The manufacturer does not document how face templates are transferred. This uses the request reported by other developers for iFace devices
		/// (command 150) and has not been verified on every model. Only reading is supported, writing face templates to a device is not.
		/// Over TCP only. <see cref="ZkTecoFace.Data"/> is kept exactly as the device returned it.
		/// </remarks>
		public List<ZkTecoFace>? GetFaces(int userIndex)
		{
			// The second byte (50) selects all faces of the user.
			var data = BitConverter.GetBytes((ushort)userIndex).Append((byte)50).ToArray();
			var packet = Command.SendCommandForData(Commands.ReadFace, data);
			var faces = new List<ZkTecoFace>();

			if (packet == null)
			{
				NotifyCommandError?.Invoke("No response from ZKTeco device while reading faces.");
				return null;
			}

			// The device refuses when the user has no face, or does not support faces.
			if (packet.Command != Commands.Success || packet.Data.Length == 0)
				return faces;

			if (packet.Data.Length % ZkTecoFace.BlockSize != 0)
			{
				NotifyCommandError?.Invoke($"Unexpected face data length {packet.Data.Length} received from ZKTeco device, expected a multiple of {ZkTecoFace.BlockSize}.");
				return null;
			}

			var index = 0;

			foreach (var block in packet.Data.Partition(ZkTecoFace.BlockSize, false))
			{
				// Empty slots are all zero in the first bytes (the size of the template).
				if (block[0] != 0 || block[1] != 0)
					faces.Add(new ZkTecoFace(userIndex, index, block));

				index++;
			}

			return faces;
		}

		/// <summary>
		/// Returns the face templates stored on the ZKTeco device for the specified user.
		/// </summary>
		/// <param name="user">The user to return faces for.</param>
		/// <inheritdoc cref="GetFaces(int)" path="/remarks" />
		public List<ZkTecoFace>? GetFaces(ZkTecoUser user) => GetFaces(user.Index);

		/// <summary>
		/// Returns all face templates stored on the ZKTeco device.
		/// </summary>
		/// <remarks>
		/// The device is asked for each user in turn, so this takes one request per user.
		/// If the device says it holds faces but none can be read, an error is reported through <see cref="NotifyCommandError"/>.
		/// See <see cref="GetFaces(int)"/> for important details.
		/// </remarks>
		public List<ZkTecoFace>? GetFaces()
		{
			var counts = GetStorageDetails();

			if (counts != null && counts.Faces == 0)
				return new List<ZkTecoFace>();

			var users = GetUsers();

			if (users == null)
				return null;

			var faces = new List<ZkTecoFace>();

			foreach (var user in users)
			{
				var found = GetFaces(user.Index);

				// No answer at all, so stop rather than waiting for every remaining user to time out.
				if (found == null)
					return null;

				faces.AddRange(found);
			}

			if (faces.Count == 0 && counts != null && counts.Faces > 0)
				NotifyCommandError?.Invoke($"ZKTeco device reports {counts.Faces} faces but none could be read. The device may not support reading faces.");

			return faces;
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
