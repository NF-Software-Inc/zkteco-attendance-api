namespace zkteco_attendance_api
{
	/// <summary>
	/// Implementation of <see cref="ICommand"/> to facilitate TCP based interactions.
	/// </summary>
	internal class TcpCommand : CommandBase<TcpPacket>, ICommand
	{
		internal TcpCommand(TcpConnection connection) : base(connection, 0xFFC0) { }
	}
}
