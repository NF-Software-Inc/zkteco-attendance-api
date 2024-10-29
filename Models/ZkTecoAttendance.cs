namespace zkteco_attendance_api;

/// <summary>
/// Defines properties of an attendance record on a ZKTeco device.
/// </summary>
/// <param name="Index">This is an internal index associated with the user, it is used in a large set of commands to refer to a given user, a 'common user' doesn't have knowledge of this number, and it may be different across different devices.</param>
/// <param name="Timestamp">The date and time the user punched in or out.</param>
/// <param name="UserId">This unique identifier for the user.</param>
/// <param name="Status">The status code for the record.</param>
/// <param name="Punch">The punch value for the record.</param>
public record class ZkTecoAttendance(int Index, DateTime Timestamp, string UserId, int Status, int Punch);
