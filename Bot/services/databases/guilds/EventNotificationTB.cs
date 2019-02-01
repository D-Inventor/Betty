using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Betty.databases.guilds
{
	public class EventNotificationTB
	{
		[Key]
		public ulong NotificationId { get; set; }
		
		public EventTB Event { get; set; }
		public DateTime DateTime { get; set; }
		public string ResponseKeyword { get; set; }
	}
}
