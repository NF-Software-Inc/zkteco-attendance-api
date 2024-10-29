using easy_core;
using System.Net;
using System.Text;

namespace zkteco_attendance_api;

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
    /// Opens a connection with the ZKTeco device.
    /// </summary>
    /// <param name="password">The password to authenticate with the ZKTeco device.</param>
    public bool Connect(int password = 0)
    {
        if (Connection.IsConnected)
            return true;

        if (Connection.Connect() == false)
            NotifyCommandError?.Invoke("Failed initializing connection to ZKTeco device.");

        var packet = Command.SendCommand(Commands.Connect, [], ZkPacketBase.DefaultHeaderLength);

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

        return packet.Command == Commands.Success;
    }

    /// <summary>
    /// Closes the connection with the ZKTeco device.
    /// </summary>
    public bool Disconnect()
    {
        if (Connection.IsConnected == false)
            return true;

        var packet = Command.SendCommand(Commands.Disconnect, [], ZkPacketBase.DefaultHeaderLength);

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
        var packet = Command.SendCommand(Commands.EnableDevice, [], ZkPacketBase.DefaultHeaderLength);

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
        var packet = Command.SendCommand(Commands.DisableDevice, [], ZkPacketBase.DefaultHeaderLength);

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
        var packet = Command.SendCommand(Commands.Restart, [], ZkPacketBase.DefaultHeaderLength);

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
        var packet = Command.SendCommand(Commands.PowerOff, [], ZkPacketBase.DefaultHeaderLength);

        if (packet == null)
        {
            NotifyCommandError?.Invoke("Failed turning off ZKTeco device.");
            return false;
        }

        return packet.Command == Commands.Success;
    }

    /// <summary>
    /// Empties any open buffers on teh ZKTeco device.
    /// </summary>
    public bool ClearBuffer()
    {
        var packet = Command.SendCommand(Commands.ClearBuffers, [], ZkPacketBase.DefaultHeaderLength);

        if (packet == null)
        {
            NotifyCommandError?.Invoke("Failed clearing buffer on ZKTeco device.");
            return false;
        }

        return packet.Command == Commands.Success;
    }

    /// <summary>
    /// Returns the currently active firmware edition on the ZKTeco device.
    /// </summary>
    public string? GetFirmwareVersion()
    {
        var packet = Command.SendCommand(Commands.FirmwareVersion, [], 1_024);

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
        var packet = Command.SendCommand(Commands.CheckStorage, [], 1_024);

        if (packet == null || packet.Command != Commands.Success || packet.Data.Length < 80)
        {
            NotifyCommandError?.Invoke("Failed getting storage details from ZKTeco device.");
            return null;
        }

        var data = packet.Data.Partition(4, false).Select(x => BitConverter.ToInt32(x, 0)).ToList();
        return new RecordCounts(data[4], data[18], data[15], data[8], data[19], data[16], data[6], data[17], data[14]);
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
            var name = Encoding.UTF8.GetString(item[11..36]).Split('\0').First();
            var card = BitConverter.ToInt32(item, 36);
            var group = Encoding.UTF8.GetString(item[41..48]).Split('\0').First();
            var id = Encoding.UTF8.GetString(item[48..]).Split('\0').First();

            users.Add(new ZkTecoUser(index, name, password, privilege, group, id, card));
        }

        return users;
    }

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

        if (size != 40)
        {
            NotifyCommandError?.Invoke($"Attendance size not supported. Supported size is 40, actual is {size}.");
            return null;
        }

        foreach (var item in packet.Data.Skip(4).Partition(size, false))
        {
            var index = BitConverter.ToUInt16(item, 0);
            var id = Encoding.UTF8.GetString(item[2..26]).Split('\0').First();
            var status = item[26];
            var time = Encoding.UTF8.GetString(item[27..31]).Split('\0').First();
            var punch = item[31];

            attendances.Add(new ZkTecoAttendance(index, ConvertDate(time), id, status, punch));
        }

        return attendances;
    }

    private DateTime ConvertDate(string date)
    {
        if (int.TryParse(date, out var remaining) == false)
            return DateTime.MinValue;

        var seconds = remaining % 60;
        remaining = (int)Math.Floor(remaining / 60.0M);

        var minutes = remaining % 60;
        remaining = (int)Math.Floor(remaining / 60.0M);

        var hours = remaining % 24;
        remaining = (int)Math.Floor(remaining / 24.0M);

        var day = (remaining % 31) + 1;
        remaining = (int)Math.Floor(remaining / 31.0M);

        var month = (remaining % 12) + 1;
        remaining = (int)Math.Floor(remaining / 12.0M);

        var year = remaining + 2_000;

        return new DateTime(year, month, day, hours, minutes, seconds);
    }
}
