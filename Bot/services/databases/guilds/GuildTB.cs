using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Betty.databases.guilds
{
	public class GuildTB
	{
		[Key]
		public ulong GuildId { get; set; }

		public string Name { get; set; }
		public string Language { get; set; }
		public ulong? Public { get; set; }
		public ulong? Notification { get; set; }
		public ApplicationTB Application { get; set; }
		public ICollection<EventTB> Events { get; set; }
	}
}
