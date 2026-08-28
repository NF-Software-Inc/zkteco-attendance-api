using zkteco_attendance_api;

namespace zkteco_configurator.Models;

/// <summary>
/// Represents the versioned contents of a device backup file.
/// </summary>
public sealed class DeviceBackupPackage
{
    /// <summary>
    /// The current version of the device backup file schema.
    /// </summary>
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>
    /// The version of the backup file schema.
    /// </summary>
    public string SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>
    /// The UTC timestamp when the backup was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Information that identifies the source ZKTeco device.
    /// </summary>
    public BackupDeviceInfo DeviceInfo { get; set; } = new();

    /// <summary>
    /// Safe device settings included in the backup.
    /// </summary>
    public BackupDeviceSettings? Settings { get; set; }

    /// <summary>
    /// List of users on the ZKTeco device being exported.
    /// </summary>
    public List<ZkTecoUser>? Users { get; set; }

    /// <summary>
    /// List of attendance records on the ZKTeco device being exported.
    /// </summary>
    public List<ZkTecoAttendance>? AttendanceRecords { get; set; }
}

/// <summary>
/// Represents identifying information about the device from which a backup was created.
/// </summary>
public sealed class BackupDeviceInfo
{
    /// <summary>
    /// The device serial number.
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// The device firmware version.
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// The device platform.
    /// </summary>
    public string? Platform { get; set; }
}

/// <summary>
/// Represents safe device settings included in a backup.
/// </summary>
public sealed class BackupDeviceSettings
{
    /// <summary>
    /// The name of the ZKTeco device.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// The current time of the ZKTeco device.
    /// </summary>
    public DateTime? DeviceTime { get; set; }

    /// <summary>
    /// The extended format of the ZKTeco device, which may include additional configuration details.
    /// </summary>
    public string? ExtendedFormat { get; set; }

    /// <summary>
    /// The extended format of the ZKTeco device for user-specific settings.
    /// </summary>
    public string? UserExtendedFormat { get; set; }

    /// <summary>
    /// The version of the face recognition feature of the ZKTeco device.
    /// </summary>
    public string? FaceVersion { get; set; }

    /// <summary>
    /// The version of the fingerprint recognition feature of the ZKTeco device.
    /// </summary>
    public string? FingerprintVersion { get; set; }

    /// <summary>
    /// The IP address of the ZKTeco device when network settings are included.
    /// </summary>
    public string? DeviceIp { get; set; }

    /// <summary>
    /// The subnet mask of the ZKTeco device when network settings are included.
    /// </summary>
    public string? SubnetMask { get; set; }

    /// <summary>
    /// The gateway IP address of the ZKTeco device when network settings are included.
    /// </summary>
    public string? GatewayIp { get; set; }

    /// <summary>
    /// The MAC address of the ZKTeco device when network settings are included.
    /// </summary>
    public string? MacAddress { get; set; }
}
