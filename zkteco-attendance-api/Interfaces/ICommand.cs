namespace zkteco_attendance_api
{
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

        /// <summary>
        /// Issues a command to the connected ZKTeco device whose response carries a block of data, and returns the complete data.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="data">The information to send with the command.</param>
        /// <returns>
        /// A packet with <see cref="Commands.Success"/> and the response data when the device returned data,
        /// the device's own response packet when it refused the command, or null when no valid response was received or the connection type does not support this operation (only TCP does).
        /// </returns>
        IZkPacket? SendCommandForData(Commands command, byte[] data);
    }

    /// <param name="message">The error message from the ZKTeco device.</param>
    public delegate void CommandError(string message);
}
