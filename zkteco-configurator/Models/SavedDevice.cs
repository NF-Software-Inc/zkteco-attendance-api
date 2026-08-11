using zkteco_attendance_api;

namespace zkteco_configurator.Models;

/// <summary>
/// View model that extends <see cref="ZkDeviceSettings"/> to store additional details for previously connected devices.
/// </summary>
public class SavedDevice : ZkDeviceSettings
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SavedDevice"/> class.
	/// </summary>
	/// <remarks>
	/// Parameterless constructor required for JSON deserialization.
	/// </remarks>
	public SavedDevice() : base(string.Empty) { }

	/// <inheritdoc cref="ZkDeviceSettings(string)" />
	public SavedDevice(string ip) : base(ip) { }

	/// <inheritdoc cref="ZkDeviceSettings(string, int, bool, int)" />
	public SavedDevice(string ip, int port, bool useTcp, int password) : base(ip, port, useTcp, password) { }

	/// <summary>
	/// A friendly name to identify the device.
	/// </summary>
	public string? NickName { get; set; }

	/// <summary>
	/// A description of the device.
	/// </summary>
	public string? Description { get; set; }
}
