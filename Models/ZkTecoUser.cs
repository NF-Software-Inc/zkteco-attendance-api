namespace zkteco_attendance_api;

/// <summary>
/// Defines properties of a user account on a ZKTeco device.
/// </summary>
/// <param name="Index">This is an internal index associated with the user, it is used in a large set of commands to refer to a given user, a 'common user' doesn't have knowledge of this number, and it may be different across different devices.</param>
/// <param name="Name">The name of the employee.</param>
/// <param name="Password">Password to access the device.</param>
/// <param name="Privilege">This sets the level of actions that a user may perform, regular employees are 'common users' while the IT admins may be 'superadmins'.</param>
/// <param name="Group">New users are by default on group 1, but there may be 100 different groups, a user can only belong to one group, they could inherit permissions and settings from the group to which they belong.</param>
/// <param name="UserId">This unique identifier for the user.</param>
/// <param name="Card">This corresponds to the number of an RFID card, this depends on the verify style.</param>
public record class ZkTecoUser(int Index, string Name, string? Password, Privilege Privilege, string? Group, string UserId, int Card);
