namespace zkteco_attendance_api;

/// <summary>
/// Implementation of <see cref="ICommand"/> to facilitate TCP based interactions.
/// </summary>
internal class TcpCommand(TcpConnection connection) : CommandBase<TcpPacket>(connection, 0xFFC0), ICommand { }