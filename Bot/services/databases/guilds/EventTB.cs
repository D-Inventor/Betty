using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Betty.databases.guilds
{
	public class EventTB
	{
		[Key]
		public ulong EventID { get; set; }

		[ForeignKey("FK_Event_Guild")]
		public GuildTB Guild { get; set; }

		public string Name { get; set; }
		public DateTime Date { get; set; }
		public ulong NotificationChannel { get; set; }
		public bool DoNotifications { get; set; }
		public ICollection<EventNotificationTB> Notifications { get; set; }
	}
}
