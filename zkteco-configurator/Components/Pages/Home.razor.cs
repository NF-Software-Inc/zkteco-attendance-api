using easy_blazor_bulma;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using zkteco_attendance_api;

namespace zkteco_configurator.Components.Pages;

public sealed partial class Home : ComponentBase, IDisposable
{
	private static string AppVersion => typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown";

	private readonly PageModel InputModel = new();
	private ZkTeco? ZkTecoClock;
	private ZkTecoUser NewUser = new();

	private readonly PlaceholderModel DeviceDetailsPlaceholder = new();

	private string? ConnectionStatusMessage;
	private string? DeviceDetailsMessage;

    private ActionMessage? UserManagementActionMessage;
	private ActionMessage? UserModalActionMessage;
	private ActionMessage? AttendanceActionMessage;
	private ActionMessage? DeviceActionMessage;

    private RecordCounts? DeviceStorageCounts;
	private readonly List<ZkTecoUser> Users = [];
	private readonly List<ZkTecoAttendance> Attendances = [];

	private bool ModalAddUserDisplayed;
	private bool ModalDeleteAttendanceDisplayed;

    #region Filter Users
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
			IEnumerable<ZkTecoUser> filteredUsers = Users;

			if (string.IsNullOrWhiteSpace(UserFilterName) == false)
				filteredUsers = filteredUsers.Where(user => user.Name.Contains(UserFilterName.Trim(), StringComparison.OrdinalIgnoreCase));

			if (UserFilterPrivilege.HasValue)
				filteredUsers = filteredUsers.Where(user => user.Privilege == UserFilterPrivilege.Value);

			if (UserFilterCard.HasValue)
				filteredUsers = filteredUsers.Where(user => user.Card == UserFilterCard.Value);

			return filteredUsers;
		}
	}
    #endregion

	/// <summary>
	/// Gets attendance rows enriched with user details from the in-memory users list.
	/// </summary>
	private IEnumerable<AttendanceDetailRow> AttendanceDetails
	{
		get
		{
            // Create a dictionary of users by UserId for quick lookup, ignoring case
            var usersByUserId = Users.Where(user => string.IsNullOrWhiteSpace(user.UserId) == false)
				.GroupBy(user => user.UserId, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

			foreach (var attendance in Attendances)
			{
				var isUserMatched = usersByUserId.TryGetValue(attendance.UserId, out var matchedUser);

				yield return new AttendanceDetailRow(
					IsUserMatched: isUserMatched,
					UserId: attendance.UserId, Timestamp: attendance.Timestamp, Status: attendance.Status, Punch: attendance.Punch,
					UserName: isUserMatched ? matchedUser!.Name : "-", UserGroup: isUserMatched ? matchedUser!.Group : null,
					UserCard: isUserMatched ? matchedUser!.Card : null, UserPrivilege: isUserMatched ? matchedUser!.Privilege : null);
			}
		}
	}

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

	/// <summary>
	/// Ensures a clock connection is available before executing device commands.
	/// </summary>
	/// <remarks>
	/// Centralizes the "not connected" status message so command methods stay concise.
	/// </remarks>
	[MemberNotNullWhen(true, nameof(ZkTecoClock))]
	private bool EnsureConnectedClock()
	{
		if (ZkTecoClock == null || ZkTecoClock.IsConnected == false)
		{
			ConnectionStatusMessage = "Not connected to ZKTeco clock.";
			return false;
		}

		return true;
	}

	private void GetDeviceDetails()
	{
		if (EnsureConnectedClock() == false)
			return;

        // Capture any command-layer errors emitted while querying details
        string? deviceDetailsCommandError = null;
		CommandError onDeviceDetailsError = message => deviceDetailsCommandError = message;
		ZkTecoClock.NotifyCommandError += onDeviceDetailsError;

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
		ZkTecoClock.NotifyCommandError -= onDeviceDetailsError;

		var success = string.IsNullOrWhiteSpace(deviceDetailsCommandError);
		SetActionMessage(ref DeviceActionMessage, "Get Device Details", success,
			success ? "loaded device details." : $"loaded partial device details. {deviceDetailsCommandError}");
	}

	private void EnableDevice()
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = ZkTecoClock.EnableDevice();
		SetActionMessage(ref DeviceActionMessage, "Enable Device", success, success ? "device enabled." : "failed enabling ZKTeco device.");
	}

	private void DisableDevice()
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = ZkTecoClock.DisableDevice();
		SetActionMessage(ref DeviceActionMessage, "Disable Device", success, success ? "device disabled." : "failed disabling ZKTeco device.");
	}

	private void RestartDevice()
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = ZkTecoClock.RestartDevice();
		SetActionMessage(ref DeviceActionMessage, "Restart Device", success, success ? "restart success." : "failed restarting ZKTeco device.");

		if (success)
			ZkTecoClock = null;
	}

	private void ShutdownDevice()
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = ZkTecoClock.ShutdownDevice();
		SetActionMessage(ref DeviceActionMessage, "Shutdown Device", success, success ? "shutdown success." : "failed turning off ZKTeco device.");

		if (success)
			ZkTecoClock = null;
	}

	private void ClearAndRefresh()
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = ZkTecoClock.ClearBuffer() & ZkTecoClock.ClearError() & ZkTecoClock.RefreshData();
		SetActionMessage(ref DeviceActionMessage, "Clear Errors and Refresh", success, success ? "cleared errors and refreshed data." : "failed clearing errors and refreshing data on ZKTeco device.");
	}

	private void SetClockTime()
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = InputModel.ClockTime != null
			? ZkTecoClock.SetTime(InputModel.ClockTime.Value)
			: ZkTecoClock.SetTime();
		SetActionMessage(ref DeviceActionMessage, "Set Device Time", success, success ? "device time updated." : "failed setting device time on ZKTeco device.");
	}

	private void SetDisplayText()
	{
		if (EnsureConnectedClock() == false)
			return;

		var displayText = string.IsNullOrWhiteSpace(InputModel.DisplayText) ? "Welcome" : InputModel.DisplayText;
		var success = ZkTecoClock.SetDisplayText(displayText);
		SetActionMessage(ref DeviceActionMessage, "Set Device Display Text", success, success ? "device display text updated." : "failed setting display text on ZKTeco device.");
	}

	private void ClearDisplayText()
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = ZkTecoClock.ClearDisplayText();
		SetActionMessage(ref DeviceActionMessage, "Clear Device Display Text", success, success ? "device display text cleared." : "failed clearing display text on ZKTeco device.");
	}

	private void GetUsers()
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = ReloadUsers();
		SetActionMessage(ref UserManagementActionMessage, "Get Users", success, success ? $"loaded {Users.Count} user(s)." : "failed reading users from the ZKTeco device.");
	}

    /// <summary>
    /// Reloads the list of users from the ZKTeco clock and updates the in-memory Users list.
    /// </summary>
	private bool ReloadUsers()
	{
		if (EnsureConnectedClock() == false)
			return false;

        Users.Clear();

		var users = ZkTecoClock.GetUsers();

		if (users == null)
			return false;

		Users.AddRange(users);
		return true;
	}

    private void OpenModalAddUser()
    {
		NewUser = new();
		UserModalActionMessage = null;
        ModalAddUserDisplayed = true;
    }

    private void CloseModalAddUser()
    {
		UserModalActionMessage = null;
        ModalAddUserDisplayed = false;
    }

    private void CreateUser()
	{
		if (EnsureConnectedClock() == false)
		{
			SetActionMessage(ref UserModalActionMessage, "Save User", false, "not connected to ZKTeco clock.");
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

            // If the user is being edited, update the existing user in the list
            else
            {
				var existingIndex = Users.FindIndex(x => x.Index == NewUser.Index);

				if (existingIndex >= 0)
					Users[existingIndex] = NewUser;
			}

			ModalAddUserDisplayed = false;
			UserModalActionMessage = null;
			NewUser = new();
			SetActionMessage(ref UserManagementActionMessage, action, true, $"saved user '{userName}'.");
		}
		else
		{
			if (add)
				NewUser.Index = 0;

			SetActionMessage(ref UserModalActionMessage, action, false, $"failed saving user '{userName}' to the ZKTeco device.");
		}
	}

    /// <summary>
    /// Opens the modal to edit an existing user by populating the NewUser model with the selected user's data.
    /// </summary>
    /// <param name="user">The user to edit.</param>
    private void EditUser(ZkTecoUser user)
	{
		NewUser = new(user.UserId, user.Name, user.Index, user.Password, user.Privilege, user.Group, user.Card);
		UserModalActionMessage = null;
		ModalAddUserDisplayed = true;
	}

	private void DeleteUser(ZkTecoUser user)
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = ZkTecoClock.DeleteUser(user);

		if (success)
			Users.Remove(user);

		SetActionMessage(ref UserManagementActionMessage, "Delete User", success, success ? $"deleted user '{user.UserId}'." : $"failed deleting user '{user.UserId}' from the ZKTeco device.");
	}

	private void GetAttendanceRecords()
	{
		if (EnsureConnectedClock() == false)
			return;

        // If users list is empty, reload it to ensure matching attendance records with user details
        if (Users.Count <= 0)
			_ = ReloadUsers();

        Attendances.Clear();

		var records = ZkTecoClock.GetAttendance();
		var success = records != null;

		if (success)
			Attendances.AddRange(records!);

		SetActionMessage(ref AttendanceActionMessage, "Get Attendance", success, success ? $"loaded {Attendances.Count} attendance record(s)." : "failed reading attendance records from the ZKTeco device.");
	}

	private void ClearAttendanceRecords()
	{
		if (EnsureConnectedClock() == false)
			return;

		var success = ZkTecoClock.ClearAttendance();

		if (success)
			Attendances.Clear();

		SetActionMessage(ref AttendanceActionMessage, "Delete Attendance", success, success ? "deleted all attendance records." : "failed deleting attendance records from the ZKTeco device.");
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
    /// <param name="detail">Additional details about the action's outcome.</param>
    /// <remarks>
    /// Successful actions append the <c>auto-hide</c> CSS class so the message fades out via
    /// <c>Home.razor.css</c>. Failed actions use <c>is-danger</c> and remain visible.
    ///
    /// A unique <c>RenderKey</c> is assigned for each message so repeated actions with similar
    /// content still trigger a fresh render and replay the CSS animation.
    /// </remarks>
    private static void SetActionMessage(ref ActionMessage? target, string action, bool success, string detail)
	{
		target = new(
			Success: success,
			Class: success ? "is-success auto-hide" : "is-danger",
			Message: $"[{action}] {(success ? "Success" : "Fail")}: {detail}",
			RenderKey: DateTime.UtcNow.Ticks);
    }

    /// <summary>
    /// Represents one attendance row enriched with user details for table display.
    /// </summary>
    private sealed record AttendanceDetailRow(bool IsUserMatched,
		string UserId, DateTime Timestamp, int Status, int Punch,
		string UserName, string? UserGroup, int? UserCard, Privilege? UserPrivilege);

    /// <summary>
    /// Represents a user-management operation message with UI style metadata.
    /// </summary>
	/// <param name="Class">The CSS class to apply for styling the message, including optional auto-hide behavior.</param>
    /// <param name="Message">The message text to display.</param>
	/// <param name="RenderKey">A unique key to force re-rendering of the message in the UI, so repeated messages can replay animations.</param>
	private sealed record ActionMessage(bool Success, string Class, string Message, long RenderKey);

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

	/// <inheritdoc />
	public void Dispose()
	{
		Reset();
	}
}
