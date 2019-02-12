using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

using Betty.utilities;

namespace Betty.databases.guilds
{
	public class PermissionTB
	{
		[Key]
		public ulong PermissionID { get; set; }
		[Required]
		public PermissionType PermissionType { get; set; }
		[Required]
		public ulong PermissionTarget { get; set; }
		[Required]
		public Permission Permission { get; set; }
		[Required]
		public GuildTB Guild { get; set; }
	}

	public enum PermissionType
	{
		User, Role
	}
}
