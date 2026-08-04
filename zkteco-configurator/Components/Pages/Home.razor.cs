using easy_blazor_bulma;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using zkteco_attendance_api;

namespace zkteco_configurator.Components.Pages;

public sealed partial class Home : ComponentBase, IDisposable
{
	private readonly PageModel InputModel = new();
	private ZkTeco? ZkTecoClock;
	private ZkTecoUser NewUser = new();

	private readonly PlaceholderModel DeviceDetailsPlaceholder = new();
	private readonly PlaceholderModel UserDetailsPlaceholder = new();

	private string? ConnectionStatusMessage;
	private string? DeviceDetailsMessage;

    private RecordCounts? DeviceStorageCounts;
	private readonly List<ZkTecoUser> Users = [];
	private readonly List<ZkTecoAttendance> Attendances = [];

	private bool DisableSubmit => string.IsNullOrWhiteSpace(InputModel.IpAddress) ||
		InputModel.Port < 1 ||
		InputModel.Port > 65_535 ||
		string.IsNullOrWhiteSpace(InputModel.Password);

	private bool DisableControls => ZkTecoClock == null || ZkTecoClock.IsConnected == false;

	private bool DisableCreateUser => string.IsNullOrWhiteSpace(NewUser.Name) ||
		string.IsNullOrWhiteSpace(NewUser.UserId);

	private readonly TooltipOptions TooltipTop = TooltipOptions.Top | TooltipOptions.HasArrow | TooltipOptions.Multiline;

	private readonly InputDateTimeOptions InputDateTimeMode =
		InputDateTimeOptions.ClickPopout |
		InputDateTimeOptions.PopoutTop |
		InputDateTimeOptions.PopoutLeft |
		InputDateTimeOptions.ShowNowButton |
		InputDateTimeOptions.ShowResetButton |
		InputDateTimeOptions.UpdateOnPopoutChange |
		InputDateTimeOptions.UseAutomaticStatusColors |
		InputDateTimeOptions.ShowDate |
		InputDateTimeOptions.ShowHours |
		InputDateTimeOptions.ShowMinutes |
		InputDateTimeOptions.ShowSeconds |
		InputDateTimeOptions.CloseOnDateClicked |
		InputDateTimeOptions.ValidateTextInput;

    /// <summary>
    /// Defines the JSON serialization options for exporting device backup data, including indentation for readability.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
	};

    /// <summary>
    /// Defines the schema version for the exported device backup package.
    /// </summary>
	/// <remarks>
	/// This versioning allows for future changes to the export format while maintaining compatibility with older versions.
	/// The schema version follows the format "major.minor" and should be incremented according to semantic versioning principles.
	/// </remarks>
    private const string ExportSchemaVersion = "1.0";

	private void OnConnect()
	{
		Reset();
		ZkTecoClock = new ZkTeco(InputModel.IpAddress!, InputModel.Port, InputModel.UseTcp);

		if (int.TryParse(InputModel.Password, out int password) == false)
		{
			ConnectionStatusMessage = "Failed parsing password as an integer.";
			ZkTecoClock = null;
		}
		else if (ZkTecoClock.Connect(password) == false)
		{
			ConnectionStatusMessage = "Failed connecting to ZKTeco clock.";
			ZkTecoClock = null;
		}
		else
		{
			ConnectionStatusMessage = "Connected!";
		}
	}

	private void GetDeviceDetails()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		DeviceDetailsMessage = string.Empty;

		DeviceDetailsMessage += "Time: " + ZkTecoClock.GetTime()?.ToString("G") + Environment.NewLine;
		DeviceDetailsMessage += "Name: " + ZkTecoClock.GetDeviceName() + Environment.NewLine;
		DeviceDetailsMessage += "IP: " + ZkTecoClock.GetDeviceIp() + Environment.NewLine;
		DeviceDetailsMessage += "Subnet: " + ZkTecoClock.GetDeviceSubnetMask() + Environment.NewLine;
		DeviceDetailsMessage += "Gateway IP: " + ZkTecoClock.GetDeviceGatewayIp() + Environment.NewLine;
		DeviceDetailsMessage += "MAC: " + ZkTecoClock.GetDeviceMac() + Environment.NewLine;
		DeviceDetailsMessage += "Serial: " + ZkTecoClock.GetDeviceSerial() + Environment.NewLine;

		DeviceDetailsMessage += "Format: " + ZkTecoClock.GetDeviceExtendedFormat() + Environment.NewLine;
		DeviceDetailsMessage += "User Format: " + ZkTecoClock.GetDeviceUserExtendedFormat() + Environment.NewLine;
		DeviceDetailsMessage += "Face Version: " + ZkTecoClock.GetDeviceFaceVersion() + Environment.NewLine;
		DeviceDetailsMessage += "Fingerprint Version: " + ZkTecoClock.GetDeviceFingerprintVersion() + Environment.NewLine;
		DeviceDetailsMessage += "Firmware Version: " + ZkTecoClock.GetFirmwareVersion() + Environment.NewLine;
		DeviceDetailsMessage += "Old Firmware Version: " + ZkTecoClock.GetDeviceOldFirmwareVersion() + Environment.NewLine;
		DeviceDetailsMessage += "Platform: " + ZkTecoClock.GetDevicePlatform() + Environment.NewLine;

		DeviceStorageCounts = ZkTecoClock.GetStorageDetails();
	}

	private void EnableDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		ZkTecoClock.EnableDevice();
	}

	private void DisableDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		ZkTecoClock.DisableDevice();
	}

	private void RestartDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		if (ZkTecoClock.RestartDevice())
			ZkTecoClock = null;
	}

	private void ShutdownDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		if (ZkTecoClock.ShutdownDevice())
			ZkTecoClock = null;
	}

	private void ClearAndRefresh()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		ZkTecoClock.ClearBuffer();
		ZkTecoClock.ClearError();
		ZkTecoClock.RefreshData();
	}

	private void SetClockTime()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		if (InputModel.ClockTime != null)
			ZkTecoClock.SetTime(InputModel.ClockTime.Value);
		else
			ZkTecoClock.SetTime();
	}

	private void SetDisplayText()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		if (string.IsNullOrWhiteSpace(InputModel.DisplayText))
			ZkTecoClock.SetDisplayText("Welcome");
		else
			ZkTecoClock.SetDisplayText(InputModel.DisplayText);
	}

	private void ClearDisplayText()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		ZkTecoClock.ClearDisplayText();
	}

	private void GetUsers()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		Users.Clear();

		var users = ZkTecoClock.GetUsers();

		if (users != null)
			Users.AddRange(users);
	}

	private void CreateUser()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var existing = ZkTecoClock.GetUsers();
		var index = existing != null && existing.Count > 0 ? existing.Max(x => x.Index) + 1 : 1;
		var add = NewUser.Index == 0;

		if (add)
			NewUser.Index = index;

		if (ZkTecoClock.CreateUser(NewUser))
		{
			if (add)
				Users.Add(NewUser);

			NewUser = new();
		}
	}

	private void EditUser(ZkTecoUser user)
	{
		NewUser = user;
	}

	private void DeleteUser(ZkTecoUser user)
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		if (ZkTecoClock.DeleteUser(user))
			Users.Remove(user);
	}

	private void GetAttendanceRecords()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		Attendances.Clear();

		var records = ZkTecoClock.GetAttendance();

		if (records != null)
			Attendances.AddRange(records);
	}

	private void ClearAttendanceRecords()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		ZkTecoClock.ClearAttendance();
	}

    /// <summary>
    /// Exports the current state of the connected ZKTeco device, including users and attendance records, to a JSON file.
    /// </summary>
    /// <returns></returns>
    private async Task ExportDeviceBackupAsync()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		try
		{
			var package = BuildDeviceExportPackage();
			var fileName = $"zkteco-device-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json";
			var json = JsonSerializer.Serialize(package, JsonOptions);

			var exported = await SaveExportJsonAsync(fileName, json);

			if (exported)
                ConnectionStatusMessage = $"Device backup exported to '{fileName}'.";
        }
        catch (Exception ex)
		{
            ConnectionStatusMessage = $"Failed exporting device backup: {ex.Message}";
        }
    }

    /// <summary>
    /// Saves the provided JSON string to a file using a file save picker dialog.
    /// </summary>
    /// <param name="fileName">The name of the file to save.</param>
    /// <param name="json">The JSON string to save.</param>
    /// <returns>True if the file was successfully saved; otherwise, false.</returns>
	/// <remarks>
	/// This method uses a file save picker dialog, which is currently supported on Windows only.
	/// </remarks>
    private async Task<bool> SaveExportJsonAsync(string fileName, string json)
	{
#if WINDOWS
		var appWindow = Application.Current?.Windows is { Count: > 0 } windows
			? windows[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window
			: null;

		if (appWindow == null)
		{
			ConnectionStatusMessage = "Unable to get active app window for export.";
			return false;
		}

		var picker = new Windows.Storage.Pickers.FileSavePicker
		{
			SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
			SuggestedFileName = Path.GetFileNameWithoutExtension(fileName),
		};

		picker.FileTypeChoices.Add("JSON file", [".json"]);

		var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(appWindow);
		WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

		var file = await picker.PickSaveFileAsync();

		if (file == null)
		{
			ConnectionStatusMessage = "Export canceled.";
			return false;
		}

		await Windows.Storage.FileIO.WriteTextAsync(file, json);
		return true;
#else
        // TODO:
        ConnectionStatusMessage = "Export with a save picker is currently supported on Windows only.";
        return false;
		#endif
	}

    /// <summary>
    /// Builds a DeviceExportPackage containing the current state of the connected ZKTeco device, including users and attendance records.
    /// </summary>
    /// <returns>The constructed DeviceExportPackage.</returns>
    private DeviceExportPackage BuildDeviceExportPackage()
	{
		var users = ZkTecoClock?.GetUsers() ?? [];
		var attendanceRecords = ZkTecoClock?.GetAttendance() ?? [];

		return new DeviceExportPackage
		{
			SchemaVersion = ExportSchemaVersion,
			ExportedAtUtc = DateTime.UtcNow,
			DeviceInfo = new DeviceExportInfo
			{
				SerialNumber = ZkTecoClock?.GetDeviceSerial(),
				FirmwareVersion = ZkTecoClock?.GetFirmwareVersion(),
				Platform = ZkTecoClock?.GetDevicePlatform(),
			},
			// Intentionally "safe settings" and avoid network identity fields.
			Settings = new DeviceSafeSettings
			{
				DeviceName = ZkTecoClock?.GetDeviceName(),
				DeviceTime = ZkTecoClock?.GetTime(),
				ExtendedFormat = ZkTecoClock?.GetDeviceExtendedFormat(),
				UserExtendedFormat = ZkTecoClock?.GetDeviceUserExtendedFormat(),
				FaceVersion = ZkTecoClock?.GetDeviceFaceVersion(),
				FingerprintVersion = ZkTecoClock?.GetDeviceFingerprintVersion(),
			},
			Users = users,
			AttendanceRecords = attendanceRecords,
		};
	}

	private void Reset()
	{
		if (ZkTecoClock != null && ZkTecoClock.IsConnected)
		{
			ZkTecoClock.Disconnect();
			ZkTecoClock = null;
		}

		ConnectionStatusMessage = null;
		DeviceDetailsMessage = null;
		DeviceStorageCounts = null;

		Users.Clear();
		Attendances.Clear();
	}

    private class PageModel
	{
		/// <summary>
		/// IP Address of the ZKTeco device to connect to.
		/// </summary>
		[Display(Name = "IP Address", Description = "IP Address of the ZKTeco device to connect to.")]
		public string? IpAddress { get; set; }

		/// <summary>
		/// Port number to use for the connection.
		/// </summary>
		[Display(Name = "Port", Description = "Port number to use for the connection.")]
		public int Port { get; set; } = 4_370;

		/// <summary>
		/// Specifies whether to use TCP or UDP for the connection.
		/// </summary>
		[Display(Name = "Use TCP", Description = "Specifies whether to use TCP or UDP for the connection.")]
		public bool UseTcp { get; set; } = true;

		/// <summary>
		/// The password to connect to the ZKTeco device.
		/// </summary>
		[Display(Name = "Password", Description = "The password to connect to the ZKTeco device.")]
		public string? Password { get; set; } = "0";

		/// <summary>
		/// The time to set on the ZKTeco device.
		/// </summary>
		[Display(Name = "Clock Time", Description = "The time to set on the ZKTeco device.")]
		public DateTime? ClockTime { get; set; } = DateTime.Now;

		/// <summary>
		/// The text to display on the ZKTeco device.
		/// </summary>
		[Display(Name = "Display Text", Description = "The text to display on the ZKTeco device.")]
		public string? DisplayText { get; set; }
	}

	private class PlaceholderModel { }

    /// <summary>
    /// Represents the structure of the exported device backup package.
    /// </summary>
    private sealed class DeviceExportPackage
	{
        /// <summary>
        /// The version of the export schema used for this package.
        /// </summary>
        public string SchemaVersion { get; set; } = ExportSchemaVersion;
        /// <summary>
        /// The UTC timestamp indicating when the export was performed.
        /// </summary>
        public DateTime ExportedAtUtc { get; set; }
        /// <summary>
        /// Information about the ZKTeco device being exported, including serial number, firmware version, and platform.
        /// </summary>
        public DeviceExportInfo DeviceInfo { get; set; } = new();
        /// <summary>
        /// Safe settings of the ZKTeco device being exported.
        /// </summary>
        public DeviceSafeSettings Settings { get; set; } = new();
		/// <summary>
		/// List of users on the ZKTeco device being exported.
		/// </summary>
		public List<ZkTecoUser> Users { get; set; } = [];
		/// <summary>
		/// List of attendance records on the ZKTeco device being exported.
		/// </summary>
		public List<ZkTecoAttendance> AttendanceRecords { get; set; } = [];
	}

    /// <summary>
    /// Represents information about the ZKTeco device being exported, including serial number, firmware version, and platform.
    /// </summary>
    private sealed class DeviceExportInfo
	{
		public string? SerialNumber { get; set; }
		public string? FirmwareVersion { get; set; }
		public string? Platform { get; set; }
	}

    /// <summary>
    /// Represents safe settings of the ZKTeco device being exported, excluding sensitive network identity fields.
    /// </summary>
    private sealed class DeviceSafeSettings
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
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Reset();
	}
}
