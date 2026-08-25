using easy_blazor_bulma;
using easy_core;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using zkteco_attendance_api;
using zkteco_configurator.Models;
using zkteco_configurator.Services;

namespace zkteco_configurator.Components.Pages;

public sealed partial class Home : EasyComponentBase, IDisposable
{
	private static string AppVersion => typeof(App).Assembly.GetName().Version?.ToString() ?? "0.0.0";

	[Inject]
	private SavedDeviceService SavedDeviceService { get; set; } = default!;

	[Inject]
	private DeviceBackupService DeviceBackupService { get; set; } = default!;

	private readonly PageModel InputModel = new();
	private ZkTeco? ZkTecoClock;

	/// <summary>
	/// Represents the user being created or edited in the modal dialog.
	/// </summary>
	private ZkTecoUser NewUser = new();

	/// <summary>
	/// Stores the original user data when editing an existing user, allowing for restoration if the edit is canceled.
	/// </summary>
	private ZkTecoUser? OriginalUser;

	private readonly PlaceholderModel DeviceDetailsPlaceholder = new();

	private string? ConnectionStatusMessage;
	private string? DeviceDetailsMessage;

	private ActionMessage? UserManagementActionMessage;
	private ActionMessage? UserModalActionMessage;
	private ActionMessage? AttendanceActionMessage;
	private ActionMessage? DeviceActionMessage;
    private ActionMessage? BackupActionMessage;
    private ActionMessage? BackupModalActionMessage;

	private RecordCounts? DeviceStorageCounts;
	private readonly List<ZkTecoUser> Users = [];
	private readonly List<AttendanceDetailRow> Attendances = [];

	private bool ModalAddUserDisplayed;
	private bool ModalDeleteAttendanceDisplayed;
	private bool ModalBackupDisplayed;
	private bool ModalConfirmImportDisplayed;
	private BackupOperationMode BackupModalMode = BackupOperationMode.Export;

	private string? BackupSelectedFileName;
	private DeviceBackupPackage? BackupLoadedPackage;

    [Display(Name = "Settings", Description = "Include the device settings.")]
    private bool BackupIncludeSettings = true;

    [Display(Name = "Users", Description = "Include the users.")]
    private bool BackupIncludeUsers = true;

    [Display(Name = "Attendance", Description = "Include the attendance records.")]
    private bool BackupIncludeAttendance = true;

	[Display(Name = "Network settings (IP/subnet/gateway/MAC)", Description = "Include the network settings.")]
	private bool BackupIncludeNetworkSettings;

    private readonly TooltipOptions BackupTooltipDisplayMode = TooltipOptions.Right | TooltipOptions.HasArrow | TooltipOptions.Multiline;
	private bool HasSelectedExportSections => BackupIncludeSettings || BackupIncludeUsers || BackupIncludeAttendance;
	private bool HasSelectedImportSections => BackupIncludeUsers;

    private string? UserFilterName;
	private Privilege? UserFilterPrivilege;
	private int? UserFilterCard;

	/// <summary>
	/// Gets the list of users filtered by the current filter settings.
	/// </summary>
	private IEnumerable<ZkTecoUser> FilteredUsers
	{
		get
		{
			var name = UserFilterName?.Trim();
			var card = UserFilterCard?.ToString();
			var predicate = PredicateBuilder.Create<ZkTecoUser>();

			if (string.IsNullOrEmpty(name) == false)
				predicate = predicate.And(user => user.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

			if (UserFilterPrivilege.HasValue)
				predicate = predicate.And(user => user.Privilege == UserFilterPrivilege.Value);

			if (string.IsNullOrEmpty(card) == false)
				predicate = predicate.And(user => user.Card.ToString().StartsWith(card, StringComparison.Ordinal));

			return Users.Where((predicate ?? PredicateBuilder.True<ZkTecoUser>()).Compile());
		}
	}

	private List<SavedDevice> SavedDevices = [];

	private bool DisableSubmit => string.IsNullOrWhiteSpace(InputModel.IpAddress) ||
		InputModel.Port < 1 ||
		InputModel.Port > 65_535 ||
		string.IsNullOrWhiteSpace(InputModel.Password);

	private bool DisableControls => ZkTecoClock == null || ZkTecoClock.IsConnected == false;

	private bool DisableCreateUser => DisableControls ||
		string.IsNullOrWhiteSpace(NewUser.Name) ||
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

    /// <inheritdoc />
    protected override void OnInitialized()
	{
		SavedDevices = SavedDeviceService.Load();
	}

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
			SaveConnectedDevice(password);
		}
	}

	private void OnDisconnect()
	{
		Reset();
	}

	private void SaveConnectedDevice(int password)
	{
		var device = SavedDevices.FirstOrDefault(x => string.Equals(x.Ip, InputModel.IpAddress, StringComparison.OrdinalIgnoreCase));

		if (device != null)
		{
			device.Password = password;
			device.Port = InputModel.Port;
			device.UseTcp = InputModel.UseTcp;
			device.NickName = InputModel.NickName;
			device.Description = InputModel.Description;
		}
		else
		{
			device = new SavedDevice(InputModel.IpAddress!, InputModel.Port, InputModel.UseTcp, password)
			{
				NickName = InputModel.NickName,
				Description = InputModel.Description
			};
		}

		if (SavedDeviceService.AddOrUpdate(device) && SavedDevices.Any(x => string.Equals(x.Ip, device.Ip, StringComparison.OrdinalIgnoreCase)) == false)
			SavedDevices.Add(device);
	}

	private void LoadSavedDevice(SavedDevice device)
	{
		if (ZkTecoClock != null && ZkTecoClock.IsConnected)
			return;

		InputModel.IpAddress = device.Ip;
		InputModel.Port = device.Port;
		InputModel.UseTcp = device.UseTcp;
		InputModel.Password = device.Password.ToString();
		InputModel.NickName = device.NickName;
		InputModel.Description = device.Description;
	}

	private void DeleteSavedDevice(SavedDevice device)
	{
		if (SavedDeviceService.Remove(device))
			SavedDevices.Remove(device);
	}

	private async Task GetDeviceDetails()
	{
		// Connection check
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		// Register error handler
		var errors = new List<string>();

		CommandError onCommandError = errors.Add;
		ZkTecoClock.NotifyCommandError += onCommandError;

		// Get details
		try
		{
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
		finally
		{
			ZkTecoClock.NotifyCommandError -= onCommandError;
		}

		// Report status
		var error = string.Join(Environment.NewLine, errors.Distinct(StringComparer.Ordinal));
		var success = errors.Count == 0;

		DeviceActionMessage = GetActionMessage("Get Device Details", success, "loaded device details.", $"loaded partial device details with communication errors:{Environment.NewLine}{error}");

		await StateHasChangedAsync();
	}

	private async Task EnableDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.EnableDevice();

		DeviceActionMessage = GetActionMessage("Enable Device", success, "device enabled.", "failed enabling ZKTeco device.");

		await StateHasChangedAsync();
	}

	private async Task DisableDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.DisableDevice();

		DeviceActionMessage = GetActionMessage("Disable Device", success, "device disabled.", "failed disabling ZKTeco device.");

		await StateHasChangedAsync();
	}

	private async Task RestartDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.RestartDevice();

		DeviceActionMessage = GetActionMessage("Restart Device", success, "restart success.", "failed restarting ZKTeco device.");

		if (success)
			ZkTecoClock = null;

		await StateHasChangedAsync();
	}

	private async Task ShutdownDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.ShutdownDevice();

		DeviceActionMessage = GetActionMessage("Shutdown Device", success, "shutdown success.", "failed turning off ZKTeco device.");

		if (success)
			ZkTecoClock = null;

		await StateHasChangedAsync();
	}

	private async Task ClearAndRefresh()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
		else if (ZkTecoClock.ClearBuffer() == false)
			DeviceActionMessage = GetActionMessage("Clear Errors and Refresh", false, failureDetail: "failed clearing the device buffer.");
		else if (ZkTecoClock.ClearError() == false)
			DeviceActionMessage = GetActionMessage("Clear Errors and Refresh", false, failureDetail: "failed clearing device errors.");
		else if (ZkTecoClock.RefreshData() == false)
			DeviceActionMessage = GetActionMessage("Clear Errors and Refresh", false, failureDetail: "failed refreshing device data.");
		else
			DeviceActionMessage = GetActionMessage("Clear Errors and Refresh", true, "cleared errors and refreshed data.");

		await StateHasChangedAsync();
	}

	private async Task SetClockTime()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = InputModel.ClockTime != null
			? ZkTecoClock.SetTime(InputModel.ClockTime.Value)
			: ZkTecoClock.SetTime();

		DeviceActionMessage = GetActionMessage("Set Device Time", success, "device time updated.", "failed setting device time on ZKTeco device.");

		await StateHasChangedAsync();
	}

	private async Task SetDisplayText()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var displayText = string.IsNullOrWhiteSpace(InputModel.DisplayText) ? "Welcome" : InputModel.DisplayText;
		var success = ZkTecoClock.SetDisplayText(displayText);

		DeviceActionMessage = GetActionMessage("Set Device Display Text", success, "device display text updated.", "failed setting display text on ZKTeco device.");

		await StateHasChangedAsync();
	}

	private async Task ClearDisplayText()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.ClearDisplayText();

		DeviceActionMessage = GetActionMessage("Clear Device Display Text", success, "device display text cleared.", "failed clearing display text on ZKTeco device.");

		await StateHasChangedAsync();
	}

	private async Task GetUsers()
	{
		await GetUsers(true);
	}

	private async Task GetUsers(bool showMessage)
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		Users.Clear();

		var users = ZkTecoClock.GetUsers();
		var success = users != null;

		if (success)
			Users.AddRange(users!);

		if (showMessage)
			UserManagementActionMessage = GetActionMessage("Get Users", success, $"loaded {Users.Count} user(s).", "failed reading users from the ZKTeco device.");

		await StateHasChangedAsync();
	}

	private void OpenModalAddUser()
	{
		NewUser = new();
		UserModalActionMessage = null;
		ModalAddUserDisplayed = true;
	}

	private void CloseModalAddUser()
	{
		// Restore user on cancel
		if (NewUser.Index != 0 && OriginalUser != null)
		{
			NewUser.UserId = OriginalUser.UserId;
			NewUser.Name = OriginalUser.Name;
			NewUser.Index = OriginalUser.Index;
			NewUser.Password = OriginalUser.Password;
			NewUser.Privilege = OriginalUser.Privilege;
			NewUser.Group = OriginalUser.Group;
			NewUser.Card = OriginalUser.Card;
		}

		// Hide modal
		UserModalActionMessage = null;
		OriginalUser = null;
		ModalAddUserDisplayed = false;
	}

	private async Task CreateUser()
	{
		// Connection check
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			UserModalActionMessage = GetActionMessage("Save User", false, failureDetail: "not connected to ZKTeco clock.");

			//await StateHasChangedAsync();
			return;
		}

		// Check for existing
		var existing = ZkTecoClock.GetUsers();
		var index = existing != null && existing.Count > 0 ? existing.Max(x => x.Index) + 1 : 1;
		var add = NewUser.Index == 0;

		if (add)
			NewUser.Index = index;

		// Save user
		var userName = NewUser.Name;
		var action = add ? "Create User" : "Update User";

		if (ZkTecoClock.CreateUser(NewUser))
		{
			if (add)
				Users.Add(NewUser);

			ModalAddUserDisplayed = false;
			UserModalActionMessage = null;

			// Cleanup and status message
			NewUser = new();
			OriginalUser = null;

			UserManagementActionMessage = GetActionMessage(action, true, $"saved user '{userName}'.");
		}
		else
		{
			UserModalActionMessage = GetActionMessage(action, false, failureDetail: $"failed saving user '{userName}' to the ZKTeco device.");
		}

		await StateHasChangedAsync();
	}

	/// <summary>
	/// Opens the modal to edit an existing user by populating the NewUser model with the selected user's data.
	/// </summary>
	/// <param name="user">The user to edit.</param>
	private void EditUser(ZkTecoUser user)
	{
		NewUser = user;
		OriginalUser = new(user.UserId, user.Name, user.Index, user.Password, user.Privilege, user.Group, user.Card);
		UserModalActionMessage = null;
		ModalAddUserDisplayed = true;
	}

	private async Task DeleteUser(ZkTecoUser user)
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.DeleteUser(user);

		if (success)
			Users.Remove(user);

		UserManagementActionMessage = GetActionMessage("Delete User", success, $"deleted user '{user.UserId}'.", $"failed deleting user '{user.UserId}' from the ZKTeco device.");

		await StateHasChangedAsync();
	}

	private async Task GetAttendanceRecords()
	{
		// Connection check
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		// Ensure users are loaded
		if (Users.Count == 0)
			await GetUsers(false);

		Attendances.Clear();

		// Get attendance records
		var records = ZkTecoClock.GetAttendance();
		var success = records != null;

		if (success)
		{
			Attendances.AddRange(records!.Select(attendance =>
			{
				var user = Users.FirstOrDefault(x => x.UserId == attendance.UserId);
				return new AttendanceDetailRow(attendance, user?.Name ?? "-", user?.Card);
			}));
		}

		// Update status
		AttendanceActionMessage = GetActionMessage("Get Attendance", success, $"loaded {Attendances.Count} attendance record(s).", "failed reading attendance records from the ZKTeco device.");

		await StateHasChangedAsync();
	}

	private async Task ClearAttendanceRecords()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.ClearAttendance();

		if (success)
			Attendances.Clear();

		AttendanceActionMessage = GetActionMessage("Delete Attendance", success, "deleted all attendance records.", "failed deleting attendance records from the ZKTeco device.");

		await StateHasChangedAsync();
	}

	private void OpenDeleteAttendanceModal()
	{
		ModalDeleteAttendanceDisplayed = true;
	}

	private void CloseDeleteAttendanceModal()
	{
		ModalDeleteAttendanceDisplayed = false;
	}

	private async Task ConfirmClearAttendanceRecords()
	{
		await ClearAttendanceRecords();

		ModalDeleteAttendanceDisplayed = false;
	}


	private void OpenBackupModalForExport()
	{
		BackupModalMode = BackupOperationMode.Export;
		BackupModalActionMessage = null;
		BackupIncludeSettings = true;
		BackupIncludeUsers = true;
		BackupIncludeAttendance = true;
		BackupIncludeNetworkSettings = true;
		ModalBackupDisplayed = true;
	}

	private void OpenBackupModalForImport()
	{
		BackupModalMode = BackupOperationMode.Import;
		BackupModalActionMessage = null;
        BackupIncludeSettings = false;
        BackupIncludeUsers = true;
        BackupIncludeAttendance = false;
        BackupIncludeNetworkSettings = false;
		BackupSelectedFileName = null;
		BackupLoadedPackage = null;
        ModalBackupDisplayed = true;
	}

	private void CloseBackupModal()
	{
		BackupModalActionMessage = null;
		BackupSelectedFileName = null;
		BackupLoadedPackage = null;
		ModalConfirmImportDisplayed = false;
		ModalBackupDisplayed = false;
	}

	/// <summary>
	/// Lets the user select a device backup file to import, then previews its contents.
	/// </summary>
	private async Task SelectBackupFileAsync()
	{
		BackupModalActionMessage = null;

		try
		{
			var selection = await DeviceBackupService.LoadBackupAsync();

			if (selection == null)
				return;

			BackupSelectedFileName = selection.FileName;
			BackupLoadedPackage = selection.Backup;
		}
		catch (Exception ex)
		{
			BackupSelectedFileName = null;
			BackupLoadedPackage = null;
            BackupModalActionMessage = GetActionMessage("Select Backup File", false, failureDetail: $"failed loading backup file: {ex.Message}");
		}
	}

	/// <summary>
	/// Opens the confirmation prompt before writing any selected backup data to the connected device.
	/// </summary>
	private void OpenConfirmImportModal()
	{
		ModalConfirmImportDisplayed = true;
	}

	private void CloseConfirmImportModal()
	{
		ModalConfirmImportDisplayed = false;
	}

    /// <summary>
    /// Restores the user-selected sections of a previously loaded device backup to the connected ZKTeco device.
    /// </summary>
    /// <remarks>
    /// The import operation is currently limited to users only.
    /// </remarks>
    private async Task ImportDeviceBackupAsync()
	{
		ModalConfirmImportDisplayed = false;

		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			BackupModalActionMessage = GetActionMessage("Import Device Backup", false, failureDetail: "not connected to ZKTeco clock.");
			return;
		}

		try
		{
            var detail = "The selected backup does not contain any users to import.";
            var failedUsersCount = 0;

            if (BackupLoadedPackage!.Users is { Count: > 0 } users)
            {
                var restoredUsersCount = 0;

                foreach (var user in users)
                {
                    if (ZkTecoClock.CreateUser(user))
                        restoredUsersCount++;
                    else
                        failedUsersCount++;
                }

				if (restoredUsersCount > 0)
				{
					detail = $"Imported {restoredUsersCount} user(s).";
					await GetUsers(false);
				}
				else
					detail = "No users were imported.";

				if (failedUsersCount > 0)
					detail += $" {failedUsersCount} user(s) could not be created.";
            }

			var success = failedUsersCount == 0;

            BackupActionMessage = GetActionMessage("Import Device Backup", success, successDetail: detail, failureDetail: detail);
			await StateHasChangedAsync();

            CloseBackupModal();
		}
		catch (Exception ex)
		{
			BackupModalActionMessage = GetActionMessage("Import Device Backup", false, failureDetail: $"failed importing device backup: {ex.Message}");
		}
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
            BackupModalActionMessage = GetActionMessage("Export Device Backup", false, failureDetail: "not connected to ZKTeco clock.");
            return;
        }

		try
		{
			var backup = BuildDeviceBackupPackage();
			var filePath = await DeviceBackupService.SaveBackupAsync(backup);

			if (filePath == null)
			{
				BackupModalActionMessage = GetActionMessage("Export Device Backup", false, failureDetail: "export canceled.");
				return;
			}

			BackupActionMessage = GetActionMessage("Export Device Backup", true, successDetail: $"Device backup exported to '{filePath}'.");
			await StateHasChangedAsync();
			CloseBackupModal();
        }
        catch (Exception ex)
		{
			BackupModalActionMessage = GetActionMessage("Export Device Backup", false, failureDetail: $"Failed exporting device backup: {ex.Message}");
        }
    }

    /// <summary>
    /// Uncheck "Network Settings" if "Settings" is unchecked.
    /// </summary>
    private void OnBackupIncludeSettingsChanged()
	{
		if (BackupIncludeSettings == false)
			BackupIncludeNetworkSettings = false;
	}

    /// <summary>
	/// Builds a DeviceBackupPackage containing the current state of the connected ZKTeco device, including users and attendance records.
    /// </summary>
	/// <returns>The constructed <see cref="DeviceBackupPackage"/>.</returns>
	private DeviceBackupPackage BuildDeviceBackupPackage()
	{
		var users = BackupIncludeUsers ? ZkTecoClock?.GetUsers() ?? [] : null;
		var attendanceRecords = BackupIncludeAttendance ? ZkTecoClock?.GetAttendance() ?? [] : null;
		var settings = BackupIncludeSettings
			? new BackupDeviceSettings
			{
				DeviceName = ZkTecoClock?.GetDeviceName(),
				DeviceTime = ZkTecoClock?.GetTime(),
				ExtendedFormat = ZkTecoClock?.GetDeviceExtendedFormat(),
				UserExtendedFormat = ZkTecoClock?.GetDeviceUserExtendedFormat(),
				FaceVersion = ZkTecoClock?.GetDeviceFaceVersion(),
				FingerprintVersion = ZkTecoClock?.GetDeviceFingerprintVersion(),
				DeviceIp = BackupIncludeNetworkSettings ? ZkTecoClock?.GetDeviceIp() : null,
				SubnetMask = BackupIncludeNetworkSettings ? ZkTecoClock?.GetDeviceSubnetMask() : null,
				GatewayIp = BackupIncludeNetworkSettings ? ZkTecoClock?.GetDeviceGatewayIp() : null,
				MacAddress = BackupIncludeNetworkSettings ? ZkTecoClock?.GetDeviceMac() : null,
			}
			: null;

		return new DeviceBackupPackage
		{
			CreatedAtUtc = DateTime.UtcNow,
			DeviceInfo = new BackupDeviceInfo
			{
				SerialNumber = ZkTecoClock?.GetDeviceSerial(),
				FirmwareVersion = ZkTecoClock?.GetFirmwareVersion(),
				Platform = ZkTecoClock?.GetDevicePlatform(),
			},
			Settings = settings,
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
		UserManagementActionMessage = null;
		AttendanceActionMessage = null;
		DeviceActionMessage = null;
		BackupModalActionMessage = null;
		BackupSelectedFileName = null;
		BackupLoadedPackage = null;
		DeviceDetailsMessage = null;
		DeviceStorageCounts = null;

		Users.Clear();
		Attendances.Clear();
		ClearUserFilters();
	}

	/// <summary>
	/// Resets all user-table filters to show the full list after reconnecting or clearing state.
	/// </summary>
	private void ClearUserFilters()
	{
		UserFilterName = null;
		UserFilterPrivilege = null;
		UserFilterCard = null;
	}

    /// <summary>
    ///	Creates an <see cref="ActionMessage"/> to represent the result of a user-management or device operation.
    /// </summary>
    /// <param name="action">The name of the action being performed.</param>
    /// <param name="success">Indicates whether the action was successful.</param>
    /// <param name="successDetail">Optional detailed message for a successful action.</param>
    /// <param name="failureDetail">Optional detailed message for a failed action.</param>
    private ActionMessage GetActionMessage(string action, bool success, string? successDetail = null, string? failureDetail = null)
	{
		var detail = success ? (successDetail ?? "action succeeded.") : (failureDetail ?? "action failed.");

		return new ActionMessage(success, success ? "is-success" : "is-danger", $"[{action}] {(success ? "Success" : "Fail")}: {detail}");
	}

    /// <summary>
    /// Represents one attendance row enriched with user details for table display.
    /// </summary>
    private sealed class AttendanceDetailRow : ZkTecoAttendance
	{
		public AttendanceDetailRow(ZkTecoAttendance attendance, string userName, int? userCard) : base(attendance.UserId, attendance.Timestamp, attendance.Index, attendance.Status, attendance.Punch)
		{
			UserName = userName;
			UserCard = userCard;
		}

		public string UserName { get; }
		public int? UserCard { get; }
	}

	/// <summary>
	/// Represents a user-management operation message with UI style metadata.
	/// </summary>
	/// <param name="Class">The CSS class to apply for styling the message, including optional auto-hide behavior.</param>
	/// <param name="Message">The message text to display.</param>
	private sealed record ActionMessage(bool Success, string Class, string Message);

	/// <summary>
	/// Defines the operation mode for the backup modal.
	/// </summary>
	private enum BackupOperationMode
	{
		Export,
		Import,
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
		/// A friendly name to identify the saved device.
		/// </summary>
		[Display(Name = "Nickname", Description = "A friendly name to identify the saved device.")]
		public string? NickName { get; set; }

		/// <summary>
		/// A description of the saved device.
		/// </summary>
		[Display(Name = "Description", Description = "A description of the saved device.")]
		public string? Description { get; set; }

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

	/// <inheritdoc />
	public void Dispose()
	{
		Reset();
	}
}
