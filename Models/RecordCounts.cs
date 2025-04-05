namespace zkteco_attendance_api
{
	/// <summary>
	/// Used to provide details on the number and availability of records on the ZKTeco device.
	/// </summary>
	public class RecordCounts
	{
		public RecordCounts() { }

		public RecordCounts(int users, int availableUsers, int maximumUsers, int records, int availableRecords, int maximumRecords, int fingers, int availableFingers, int maximumFingers)
		{
			Users = users;
			AvailableUsers = availableUsers;
			MaximumUsers = maximumUsers;
			Records = records;
			AvailableRecords = availableRecords;
			MaximumRecords = maximumRecords;
			Fingers = fingers;
			AvailableFingers = availableFingers;
			MaximumFingers = maximumFingers;
		}

		/// <summary>
		/// The number of user accounts on the device.
		/// </summary>
		public int Users { get; set; }

		/// <summary>
		/// The number of user accounts that can be created on the device.
		/// </summary>
		public int AvailableUsers { get; set; }

		/// <summary>
		/// The maximum number of user accounts on the device.
		/// </summary>
		public int MaximumUsers { get; set; }

		/// <summary>
		/// The number of attendance records on the device.
		/// </summary>
		public int Records { get; set; }

		/// <summary>
		/// The number of attendance records that can be created on the device.
		/// </summary>
		public int AvailableRecords { get; set; }

		/// <summary>
		/// The maximum number of attendance records on the device.
		/// </summary>
		public int MaximumRecords { get; set; }

		/// <summary>
		/// The number of fingerprints on the device.
		/// </summary>
		public int Fingers { get; set; }

		/// <summary>
		/// The number of fingerprints that can be created on the device.
		/// </summary>
		public int AvailableFingers { get; set; }

		/// <summary>
		/// The maximum number of fingerprints on the device.
		/// </summary>
		public int MaximumFingers { get; set; }
	}
}
