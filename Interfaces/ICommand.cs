namespace zkteco_attendance_api;

/// <summary>
/// Defines properties and methods required to interact with a ZKTeco device.
/// </summary>
internal interface ICommand
{
	/// <summary>
	/// Notifies an error occurred during communications with the ZKTeco device.
	/// </summary>
	event CommandError? NotifyCommandError;

	/// <summary>
	/// Starts a session on the ZKTeco device.
	/// </summary>
	/// <param name="password">The password to authenticate to the device with.</param>
	bool StartSession(int password);

	/// <summary>
	/// Issues a command to the connected ZKTeco device and returns the response.
	/// </summary>
	/// <param name="command">The command to send.</param>
	/// <param name="length">The amount of data to receive on the response.</param>
	/// <param name="data">The information to send with the command.</param>
	byte[]? SendCommand(Commands command, int length, string? data = null);
}

/// <param name="message">The error message from the ZKTeco device.</param>
public delegate void CommandError(string message);
