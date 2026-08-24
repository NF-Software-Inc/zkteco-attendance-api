using easy_blazor_bulma;
using easy_core;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using zkteco_attendance_api;
using zkteco_configurator.Models;
using zkteco_configurator.Services;

namespace zkteco_configurator.Components.Pages;

public sealed partial class Home : ComponentBase, IDisposable
{
	private static string AppVersion => typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown";

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
    private bool DisableBackup => BackupIncludeSettings == false &&
		BackupIncludeUsers == false && BackupIncludeAttendance == false;

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
            var cardPrefix = UserFilterCard?.ToString();

			var userFilterPredicate = PredicateBuilder.Create<ZkTecoUser>();

			if (string.IsNullOrWhiteSpace(name) == false)
				userFilterPredicate = userFilterPredicate.And(user => user.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

			if (UserFilterPrivilege.HasValue)
				userFilterPredicate = userFilterPredicate.And(user => user.Privilege == UserFilterPrivilege.Value);

			if (string.IsNullOrWhiteSpace(cardPrefix) == false)
				userFilterPredicate = userFilterPredicate.And(user => user.Card.ToString().StartsWith(cardPrefix, StringComparison.Ordinal));


			return Users.Where((userFilterPredicate ?? PredicateBuilder.True<ZkTecoUser>()).Compile());
        }
	}

	private List<SavedDevice> SavedDevices = [];

	private bool DisableSubmit => string.IsNullOrWhiteSpace(InputModel.IpAddress) ||
		InputModel.Port < 1 ||
		InputModel.Port > 65_535 ||
		string.IsNullOrWhiteSpace(InputModel.Password);

	private bool DisableControls => ZkTecoClock == null || ZkTecoClock.IsConnected == false;

	private bool DisableCreateUser => DisableControls || string.IsNullOrWhiteSpace(NewUser.Name) ||
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

	private void GetDeviceDetails()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

        // Capture any command-layer errors emitted while querying details
        var deviceDetailsCommandErrors = new List<string>();
		CommandError onDeviceDetailsError = deviceDetailsCommandErrors.Add;
		ZkTecoClock.NotifyCommandError += onDeviceDetailsError;

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
			ZkTecoClock.NotifyCommandError -= onDeviceDetailsError;
		}

		var commandErrorDetails = string.Join(Environment.NewLine, deviceDetailsCommandErrors.Distinct(StringComparer.Ordinal));
		var success = deviceDetailsCommandErrors.Count == 0;
		SetActionMessage(ref DeviceActionMessage, action: "Get Device Details", success: success,
			successDetail: "loaded device details.", failureDetail: $"loaded partial device details with communication errors:{Environment.NewLine}{commandErrorDetails}");
	}

	private void EnableDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.EnableDevice();
		SetActionMessage(ref DeviceActionMessage, action: "Enable Device", success: success,
			successDetail: "device enabled.", failureDetail: "failed enabling ZKTeco device.");
	}

	private void DisableDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.DisableDevice();
		SetActionMessage(ref DeviceActionMessage, action: "Disable Device", success: success,
			successDetail: "device disabled.", failureDetail: "failed disabling ZKTeco device.");
	}

	private void RestartDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.RestartDevice();
		SetActionMessage(ref DeviceActionMessage, action: "Restart Device", success: success,
			successDetail: "restart success.", failureDetail: "failed restarting ZKTeco device.");

		if (success)
			ZkTecoClock = null;
	}

	private void ShutdownDevice()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.ShutdownDevice();
		SetActionMessage(ref DeviceActionMessage, action: "Shutdown Device", success: success,
			successDetail: "shutdown success.", failureDetail: "failed turning off ZKTeco device.");

		if (success)
			ZkTecoClock = null;
	}

	private void ClearAndRefresh()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		if (ZkTecoClock.ClearBuffer() == false)
		{
			SetActionMessage(ref DeviceActionMessage, action: "Clear Errors and Refresh", success: false,
				failureDetail: "failed clearing the device buffer.");
			return;
		}

		if (ZkTecoClock.ClearError() == false)
		{
			SetActionMessage(ref DeviceActionMessage, action: "Clear Errors and Refresh", success: false,
				failureDetail: "failed clearing device errors.");
			return;
		}

		if (ZkTecoClock.RefreshData() == false)
		{
			SetActionMessage(ref DeviceActionMessage, action: "Clear Errors and Refresh", success: false,
				failureDetail: "failed refreshing device data.");
			return;
		}

		SetActionMessage(ref DeviceActionMessage, action: "Clear Errors and Refresh", success: true,
			successDetail: "cleared errors and refreshed data.");
	}

	private void SetClockTime()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = InputModel.ClockTime != null
			? ZkTecoClock.SetTime(InputModel.ClockTime.Value)
			: ZkTecoClock.SetTime();

        SetActionMessage(ref DeviceActionMessage, action: "Set Device Time", success: success,
			successDetail: "device time updated.", failureDetail: "failed setting device time on ZKTeco device.");
    }

	private void SetDisplayText()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var displayText = string.IsNullOrWhiteSpace(InputModel.DisplayText) ? "Welcome" : InputModel.DisplayText;
		var success = ZkTecoClock.SetDisplayText(displayText);
		SetActionMessage(ref DeviceActionMessage, action: "Set Device Display Text", success: success,
			successDetail: "device display text updated.", failureDetail: "failed setting display text on ZKTeco device.");
	}

	private void ClearDisplayText()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.ClearDisplayText();
		SetActionMessage(ref DeviceActionMessage, action: "Clear Device Display Text", success: success,
			successDetail: "device display text cleared.", failureDetail: "failed clearing display text on ZKTeco device.");
	}

    private void GetUsers()
    {
		GetUsers(true);
    }

    private void GetUsers(bool showMessage)
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
			SetActionMessage(ref UserManagementActionMessage, action: "Get Users", success: success,
				successDetail: $"loaded {Users.Count} user(s).", failureDetail: "failed reading users from the ZKTeco device.");
	}

    private void OpenModalAddUser()
    {
		NewUser = new();
		UserModalActionMessage = null;
        ModalAddUserDisplayed = true;
    }

    private void CloseModalAddUser()
    {
        // If the user was being edited, restore the original values to NewUser
        if (NewUser.Index != 0 && OriginalUser != null)
		{
			NewUser.UserId = OriginalUser.UserId;
			NewUser.Name = OriginalUser.Name;
			NewUser.Index = OriginalUser.Index;
			NewUser.Password = OriginalUser.Password;
			NewUser.Privilege = OriginalUser.Privilege;
			NewUser.Group = OriginalUser.Group;
			NewUser.Card = OriginalUser.Card;

            // Clear the original user reference after restoring values
            OriginalUser = null;
        }

        UserModalActionMessage = null;
        ModalAddUserDisplayed = false;
    }

    private void CreateUser()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			SetActionMessage(ref UserModalActionMessage, action: "Save User", success: false,
				failureDetail: "not connected to ZKTeco clock.");
			return;
		}

		var existing = ZkTecoClock.GetUsers();
        var index = existing != null && existing.Count > 0 ? existing.Max(x => x.Index) + 1 : 1;
		var add = NewUser.Index == 0;

		if (add)
			NewUser.Index = index;

		var userName = NewUser.Name;
		var action = add ? "Create User" : "Update User";

		if (ZkTecoClock.CreateUser(NewUser))
		{
			if (add)
				Users.Add(NewUser);

			ModalAddUserDisplayed = false;
			UserModalActionMessage = null;

            // Reset NewUser and OriginalUser after successful save
            NewUser = new();
			OriginalUser = null;

            SetActionMessage(ref UserManagementActionMessage, action: action, success: true,
				successDetail: $"saved user '{userName}'.");
		}
		else
			SetActionMessage(ref UserModalActionMessage, action: action, success: false,
				failureDetail: $"failed saving user '{userName}' to the ZKTeco device.");
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

	private void DeleteUser(ZkTecoUser user)
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.DeleteUser(user);

		if (success)
			Users.Remove(user);

		SetActionMessage(ref UserManagementActionMessage, action: "Delete User", success: success,
			successDetail: $"deleted user '{user.UserId}'.", failureDetail: $"failed deleting user '{user.UserId}' from the ZKTeco device.");
	}

    private void GetAttendanceRecords()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		// If users list is empty, reload it to ensure matching attendance records with user details
		if (Users.Count == 0)
			GetUsers(showMessage: false);

        Attendances.Clear();

		var records = ZkTecoClock.GetAttendance();
		var success = records != null;

		if (success)
		{
            // Create a dictionary of users by UserId for quick lookup, ignoring case
            var usersByUserId = Users.Where(user => string.IsNullOrWhiteSpace(user.UserId) == false)
				.GroupBy(user => user.UserId, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

			Attendances.AddRange(records!.Select(attendance =>
			{
				var isUserMatched = usersByUserId.TryGetValue(attendance.UserId, out var matchedUser);

				return new AttendanceDetailRow(
					attendance: attendance,
					userName: isUserMatched ? matchedUser!.Name : "-",
					userCard: isUserMatched ? matchedUser!.Card : null);
			}));
		}

		SetActionMessage(ref AttendanceActionMessage, action: "Get Attendance", success: success,
			successDetail: $"loaded {Attendances.Count} attendance record(s).", failureDetail: "failed reading attendance records from the ZKTeco device.");
	}

	private void ClearAttendanceRecords()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return;
		}

		var success = ZkTecoClock.ClearAttendance();

		if (success)
			Attendances.Clear();

		SetActionMessage(ref AttendanceActionMessage, action: "Delete Attendance", success: success,
			successDetail: "deleted all attendance records.", failureDetail: "failed deleting attendance records from the ZKTeco device.");
	}

	private void OpenDeleteAttendanceModal()
	{
		ModalDeleteAttendanceDisplayed = true;
	}

	private void CloseDeleteAttendanceModal()
	{
		ModalDeleteAttendanceDisplayed = false;
	}

	private void ConfirmClearAttendanceRecords()
	{
		ClearAttendanceRecords();
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
			SetActionMessage(ref BackupModalActionMessage, action: "Select Backup File", success: false, failureDetail: $"failed loading backup file: {ex.Message}");
		}
	}

	/// <summary>
	/// Opens the confirmation prompt before writing any selected backup data to the connected device.
	/// </summary>
	private void OpenConfirmImportModal()
	{
		if (BackupLoadedPackage == null)
		{
			SetActionMessage(ref BackupModalActionMessage, action: "Import Device Backup", success: false, failureDetail: "select a backup file first.");
			return;
		}

		if (DisableBackup)
		{
			SetActionMessage(ref BackupModalActionMessage, action: "Import Device Backup", success: false, failureDetail: "select at least one section to import.");
			return;
		}

		ModalConfirmImportDisplayed = true;
	}

	private void CloseConfirmImportModal()
	{
		ModalConfirmImportDisplayed = false;
	}

	/// <summary>
	/// Restores the user-selected sections of a previously loaded device backup to the connected ZKTeco device.
	/// </summary>
	private void ImportDeviceBackupAsync()
	{
		ModalConfirmImportDisplayed = false;

		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			SetActionMessage(ref BackupModalActionMessage, action: "Import Device Backup", success: false,
				failureDetail: "not connected to ZKTeco clock.");
			return;
		}

		if (BackupLoadedPackage == null)
		{
			SetActionMessage(ref BackupModalActionMessage, action: "Import Device Backup", success: false, failureDetail: "select a backup file first.");
			return;
		}

		try
		{
			var restored = new List<string>();
			var skipped = new List<string>();

			if (BackupIncludeUsers)
			{
				if (BackupLoadedPackage.Users is { Count: > 0 } users)
				{
					foreach (var user in users)
						ZkTecoClock.CreateUser(user);

					restored.Add("users");
					GetUsers(false);
				}
				else
					skipped.Add("users (not present in backup)");
			}

			var detail = restored.Count > 0 ? $"restored {string.Join(", ", restored)}." : "no sections were restored.";

			if (skipped.Count > 0)
				detail += $" Skipped: {string.Join(", ", skipped)}.";

			SetActionMessage(ref BackupActionMessage, action: "Import Device Backup", success: true, successDetail: detail);
			CloseBackupModal();
		}
		catch (Exception ex)
		{
			SetActionMessage(ref BackupModalActionMessage, action: "Import Device Backup", success: false, failureDetail: $"failed importing device backup: {ex.Message}");
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
            SetActionMessage(ref BackupModalActionMessage, action: "Export Device Backup", success: false,
                failureDetail: "not connected to ZKTeco clock.");
            return;
        }

        if (DisableBackup)
		{
			SetActionMessage(ref BackupModalActionMessage, action: "Export Device Backup", success: false, failureDetail: "select at least one section to export.");
			return;
		}

		try
		{
			var backup = BuildDeviceBackupPackage();
			var filePath = await DeviceBackupService.SaveBackupAsync(backup);

			if (filePath == null)
			{
				SetActionMessage(ref BackupModalActionMessage, action: "Export Device Backup", success: false, failureDetail: "export canceled.");
				return;
			}

			SetActionMessage(ref BackupActionMessage, action: "Export Device Backup", success: true, successDetail: $"Device backup exported to '{filePath}'.");
			CloseBackupModal();
        }
        catch (Exception ex)
		{
			SetActionMessage(ref BackupModalActionMessage, action: "Export Device Backup", success: false, failureDetail: $"Failed exporting device backup: {ex.Message}");
        }
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
    /// Sets a standardized status message in the provided operation message target.
    /// </summary>
    /// <param name="target">The target action message to set.</param>
    /// <param name="action">The name of the action being performed.</param>
    /// <param name="success">Indicates whether the action was successful.</param>
    /// <param name="successDetail">Optional detailed message for a successful action.</param>
    /// <param name="failureDetail">Optional detailed message for a failed action.</param>
    /// <remarks>
    /// The <c>ref</c> modifier is required because this method replaces the ActionMessage reference.
    ///  Without <c>ref</c>, only the local parameter would be reassigned and the caller's state would not be updated.
    /// </remarks>
    private void SetActionMessage(ref ActionMessage? target, string action, bool success, string? successDetail = null, string? failureDetail = null)
	{
		var detail = success ? (successDetail ?? "action succeeded.") : (failureDetail ?? "action failed.");

        target = new(
			Success: success,
			Class: success ? "is-success" : "is-danger",
			Message: $"[{action}] {(success ? "Success" : "Fail")}: {detail}");
    }

    /// <summary>
    /// Represents one attendance row enriched with user details for table display.
    /// </summary>
    private sealed class AttendanceDetailRow : ZkTecoAttendance
	{
		public AttendanceDetailRow(ZkTecoAttendance attendance, string userName, int? userCard)
			: base(attendance.UserId, attendance.Timestamp, attendance.Index, attendance.Status, attendance.Punch)
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
