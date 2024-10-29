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
    /// Issues a command to the connected ZKTeco device and returns the response.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="data">The information to send with the command.</param>
    /// <param name="length">The amount of data to receive on the response.</param>
    IZkPacket? SendCommand(Commands command, string? data, int length);

    /// <summary>
    /// Issues a command to the connected ZKTeco device and returns the response.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="data">The information to send with the command.</param>
    /// <param name="length">The amount of data to receive on the response.</param>
    IZkPacket? SendCommand(Commands command, byte[] data, int length);

    /// <summary>
    /// Issues a command the the connected ZKTeco device for a buffered read operation and returns the response.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="data">The information to send with the command.</param>
    IZkPacket? SendBufferedCommand(Commands command, byte[] data);
}

/// <param name="message">The error message from the ZKTeco device.</param>
public delegate void CommandError(string message);
