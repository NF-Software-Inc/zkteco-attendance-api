namespace zkteco_attendance_api;

/// <summary>
/// Defines properties required to connect to a ZKTeco device.
/// </summary>
/// <param name="Ip">The IP address of the device.</param>
/// <param name="Port">The port used for communication with the device.</param>
/// <param name="UseTcp">Specifies whether to use TCP or UDP for communication.</param>
/// <param name="Password">The password to authenticate with the device.</param>
public record class ZkDeviceSettings(string Ip, int Port = 4_370, bool UseTcp = true, int Password = 0);
