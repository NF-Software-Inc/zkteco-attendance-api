namespace zkteco_attendance_api;

/// <summary>
/// Defines responses that are returned from ZKTeco devices.
/// </summary>
internal enum Responses
{
	/// <summary>
	/// The command executed as expected.
	/// </summary>
	Success = 2_000,

	/// <summary>
	/// An error occurred during command execution.
	/// </summary>
	Error = 2_001,

	/// <summary>
	/// Return data is available after executing the command.
	/// </summary>
	Data = 2_002
}
