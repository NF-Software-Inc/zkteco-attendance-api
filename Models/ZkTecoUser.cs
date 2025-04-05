namespace zkteco_attendance_api
{
	/// <summary>
	/// Defines properties of a user account on a ZKTeco device.
	/// </summary>
	public class ZkTecoUser
	{
		public ZkTecoUser() 
		{
			Name = string.Empty;
			UserId = string.Empty;
		}

		public ZkTecoUser(string userId, string name, int index, string? password, Privilege privilege, string? group, int card)
		{
			UserId = userId;
			Name = name;
			Index = index;
			Password = password;
			Privilege = privilege;
			Group = group;
			Card = card;
		}

		/// <summary>
		/// This is an internal index associated with the user, it is used in a large set of commands to refer to a given user, a 'common user' doesn't have knowledge of this number, and it may be different across different devices.
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		/// The name of the employee.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Password to access the device.
		/// </summary>
		public string? Password { get; set; }

		/// <summary>
		/// This sets the level of actions that a user may perform, regular employees are 'common users' while the IT admins may be 'superadmins'.
		/// </summary>
		public Privilege Privilege { get; set; }

		/// <summary>
		/// New users are by default on group 1, but there may be 100 different groups, a user can only belong to one group, they could inherit permissions and settings from the group to which they belong.
		/// </summary>
		public string? Group { get; set; }

		/// <summary>
		/// This unique identifier for the user.
		/// </summary>
		public string UserId { get; set; }

		/// <summary>
		/// This corresponds to the number of an RFID card, this depends on the verify style.
		/// </summary>
		public int Card { get; set; }
	}
}
