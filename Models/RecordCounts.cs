namespace zkteco_attendance_api;

/// <summary>
/// Used to provide details on the number and availability of records on the ZKTeco device.
/// </summary>
/// <param name="Users">The number of user accounts on the device.</param>
/// <param name="AvailableUsers">The number of user accounts that can be created on the device.</param>
/// <param name="MaximumUsers">The maximum number of user accounts on the device.</param>
/// <param name="Records">The number of attendance records on the device.</param>
/// <param name="AvailableRecords">The number of attendance records that can be created on the device.</param>
/// <param name="MaximumRecords">The maximum number of attendance records on the device.</param>
/// <param name="Fingers">The number of fingerprints on the device.</param>
/// <param name="AvailableFingers">The number of fingerprints that can be created on the device.</param>
/// <param name="MaximumFingers">The maximum number of fingerprints on the device.</param>
public record class RecordCounts(int Users, int AvailableUsers, int MaximumUsers, int Records, int AvailableRecords, int MaximumRecords, int Fingers, int AvailableFingers, int MaximumFingers);
