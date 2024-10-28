namespace zkteco_attendance_api;

/// <summary>
/// Implementation of <see cref="ICommand"/> to facilitate UDP based interactions.
/// </summary>
internal class UdpCommand(UdpConnection connection) : CommandBase<UdpPacket>(connection, 16_384), ICommand { }
