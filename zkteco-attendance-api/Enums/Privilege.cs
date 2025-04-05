namespace zkteco_attendance_api
{
	/// <summary>
	/// A listing of user access options.
	/// </summary>
	public enum Privilege
	{
		/// <summary>
		/// Default user access. Can use the device to record attendance.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Can associate users with RFID cards, face scans, and fingerprints.
		/// </summary>
		Enroller = 2,

		/// <summary>
		/// Can manage the device settings.
		/// </summary>
		Manager = 6,

		/// <summary>
		/// Can manage the device settings, user access, and all other configuration.
		/// </summary>
		Admin = 14
	}
}
