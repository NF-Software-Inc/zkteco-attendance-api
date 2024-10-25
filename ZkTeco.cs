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

    private const int DefaultHeaderLength = 8;
    private const int DefaultResponseLength = 8;

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
            NotifyCommandError?.Invoke("Failed initializing TCP connection to ZKTeco device.");

        var result = Command.SendCommand(Commands.Connect, [], DefaultResponseLength);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed starting session with ZKTeco device.");
            return false;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        Connection.ConnectionId = header.ConnectionId;
        Connection.ResponseId = header.ResponseId;

        if (header.Command != Commands.Unauthorized)
            return true;

        result = Command.SendCommand(Commands.Authenticate, Functions.GeneratePassword(password, Connection.ConnectionId), DefaultResponseLength);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed requesting authentication with ZKTeco device.");
            return false;
        }

        header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        Connection.ResponseId = header.ResponseId;

        if (header.Command != Commands.Success)
            NotifyCommandError?.Invoke("Failed authenticating with ZKTeco device.");

        return header.Command == Commands.Success;
    }

    /// <summary>
    /// Closes the connection with the ZKTeco device.
    /// </summary>
    public bool Disconnect()
    {
        if (Connection.IsConnected == false)
            return true;

        var result = Command.SendCommand(Commands.Disconnect, [], DefaultResponseLength);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed closing session with ZKTeco device.");
            return false;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        if (header.Command == Commands.Success)
        {
            Connection.ConnectionId = 0;
            Connection.ResponseId = ushort.MaxValue - 1;
        }
        else
        {
            NotifyCommandError?.Invoke("Failed closing session with ZKTeco device.");
        }

        return header.Command == Commands.Success && Connection.Disconnect();
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
        var result = Command.SendCommand(Commands.ReadConfiguration, parameter, 1_024);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed getting configuration detail from ZKTeco device.");
            return null;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        Connection.ResponseId = header.ResponseId;

        if (header.Command != Commands.Success)
        {
            NotifyCommandError?.Invoke("Failed getting configuration detail from ZKTeco device.");
            return null;
        }

        return Encoding.UTF8.GetString(result[DefaultHeaderLength..]).Split('=').Last();
    }

    /// <summary>
    /// Enables the connected ZKTeco device and allows user activity.
    /// </summary>
    public bool EnableDevice()
    {
        var result = Command.SendCommand(Commands.EnableDevice, [], DefaultResponseLength);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed enabling ZKTeco device.");
            return false;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        Connection.ResponseId = header.ResponseId;
        return header.Command == Commands.Success;
    }

    /// <summary>
    /// Locks the ZKTeco device to prevent user activity.
    /// </summary>
    public bool DisableDevice()
    {
        var result = Command.SendCommand(Commands.DisableDevice, [], DefaultResponseLength);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed disabling ZKTeco device.");
            return false;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        Connection.ResponseId = header.ResponseId;
        return header.Command == Commands.Success;
    }
    
    /// <summary>
    /// Reboots the ZKTeco device.
    /// </summary>
    public bool RestartDevice()
    {
        var result = Command.SendCommand(Commands.Restart, [], DefaultResponseLength);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed restarting ZKTeco device.");
            return false;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        Connection.ResponseId = header.ResponseId;
        return header.Command == Commands.Success;
    }

    /// <summary>
    /// Turns off the ZKTeco device.
    /// </summary>
    public bool ShutdownDevice()
    {
        var result = Command.SendCommand(Commands.PowerOff, [], DefaultResponseLength);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed turning off ZKTeco device.");
            return false;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        Connection.ResponseId = header.ResponseId;
        return header.Command == Commands.Success;
    }
    
    /// <summary>
    /// Empties any open buffers on teh ZKTeco device.
    /// </summary>
    public bool ClearBuffer()
    {
        var result = Command.SendCommand(Commands.ClearBuffers, [], DefaultResponseLength);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed clearing buffer on ZKTeco device.");
            return false;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        Connection.ResponseId = header.ResponseId;
        return header.Command == Commands.Success;
    }

    /// <summary>
    /// Returns the currently active firmware edition on the ZKTeco device.
    /// </summary>
    public string? GetFirmwareVersion()
    {
        var result = Command.SendCommand(Commands.FirmwareVersion, [], 1_024);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed getting firmware version from ZKTeco device.");
            return null;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);

        Connection.ResponseId = header.ResponseId;
        return Encoding.UTF8.GetString(result[DefaultHeaderLength..]);
    }
    
    /// <summary>
    /// Returns the current counts of stored items on the ZKTeco device.
    /// </summary>
    public RecordCounts? GetStorageDetails()
    {
        var result = Command.SendCommand(Commands.CheckStorage, [], 1_024);

        if (result == null || result.Length < DefaultHeaderLength)
        {
            NotifyCommandError?.Invoke("Failed getting storage details from ZKTeco device.");
            return null;
        }

        var header = ZkPacketBase.ParseHeader(result[..DefaultHeaderLength]);
        var data = result[DefaultHeaderLength..].Partition(4, false).Select(x => BitConverter.ToInt32(x, 0)).ToList();

        Connection.ResponseId = header.ResponseId;

        if (header.Command != Commands.Success || data.Count < 20)
        {
            NotifyCommandError?.Invoke("Failed getting storage details from ZKTeco device.");
            return null;
        }

        return new RecordCounts(data[4], data[18], data[15], data[8], data[19], data[16], data[6], data[17], data[14]);
    }

    /// <summary>
    /// Returns a list of users that exist on the ZKTeco device.
    /// </summary>
    public List<ZkTecoUser>? GetUsers()
    {
        return null;
    }
}
