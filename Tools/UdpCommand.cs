namespace zkteco_attendance_api
{
	/// <summary>
	/// Implementation of <see cref="ICommand"/> to facilitate UDP based interactions.
	/// </summary>
	internal class UdpCommand : CommandBase<UdpPacket>, ICommand 
	{
		internal UdpCommand(UdpConnection connection) : base(connection, 16_384) { }
	}
}
