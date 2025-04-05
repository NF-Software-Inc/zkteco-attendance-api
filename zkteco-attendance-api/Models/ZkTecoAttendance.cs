using System;

namespace zkteco_attendance_api
{
	/// <summary>
	/// Defines properties of an attendance record on a ZKTeco device.
	/// </summary>
	public class ZkTecoAttendance
	{
		public ZkTecoAttendance()
		{
			UserId = string.Empty;
			Timestamp = DateTime.MinValue;
		}

		public ZkTecoAttendance(string userId, DateTime timestamp, int index, int status, int punch)
		{
			UserId = userId;
			Timestamp = timestamp;
			Index = index;
			Status = status;
			Punch = punch;
		}

		/// <summary>
		/// This is an internal index associated with the user, it is used in a large set of commands to refer to a given user, a 'common user' doesn't have knowledge of this number, and it may be different across different devices.
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		/// The date and time the user punched in or out.
		/// </summary>
		public DateTime Timestamp { get; set; }

		/// <summary>
		/// This unique identifier for the user.
		/// </summary>
		public string UserId { get; set; }

		/// <summary>
		/// The status code for the record.
		/// </summary>
		public int Status { get; set; }

		/// <summary>
		/// The punch value for the record.
		/// </summary>
		public int Punch { get; set; }
	}
}
