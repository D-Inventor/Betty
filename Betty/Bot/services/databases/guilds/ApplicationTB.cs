using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Betty.databases.guilds
{
	public class ApplicationTB
	{
		[Key]
		public ulong ApplicationId { get; set; }
		[Required]
		public GuildTB Guild { get; set; }
		[Required]
		public ulong Channel { get; set; }
		[Required]
		public string InviteID { get; set; }
		public DateTime Deadline { get; set; }
	}
}
