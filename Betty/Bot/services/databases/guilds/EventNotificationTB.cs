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
		
		[Required]
		public EventTB Event { get; set; }
		[Required]
		public DateTime Date { get; set; }
		[Required]
		public string ResponseKeyword { get; set; }
	}
}
