using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.WebSocket;
using Betty.utilities;
using Betty.databases.guilds;

namespace Betty
{
	class Agenda
	{
		Logger logger;
		GuildDB database;
		StateCollection statecollection;

		public Agenda(IServiceProvider services)
		{
			logger = services.GetRequiredService<Logger>();
			database = services.GetRequiredService<GuildDB>();
			statecollection = services.GetRequiredService<StateCollection>();
		}

		public async Task<bool> Plan(SocketGuild guild, string name, DateTime date, SocketTextChannel channel = null, bool doNotifications = true, TimeSpan[] notifications = null)
		{
			// Get the data for this guild
			GuildTB g = statecollection.GetGuildEntry(guild);

			// first plan the event
			EventTB ev = new EventTB
			{
				Guild = g,
				Name = name,
				Date = date,
				NotificationChannel = channel != null ? (ulong?)channel.Id : null,
			};

			await database.Events.AddAsync(ev);

			// if there is interest for notifications, plan those as well
			if (doNotifications)
			{
				// first plan the notification for the deadline itself
				List<EventNotificationTB> notcollection = new List<EventNotificationTB>(1)
				{
					new EventNotificationTB
					{
						DateTime = date,
						Event = ev,
						ResponseKeyword = "notifications.deadline",
					}
				};

				//then add all the other notifications
				if (notifications != null) notcollection.AddRange
						(from n in notifications
						 select new EventNotificationTB
						 {
						 	DateTime = date - n,
						 	Event = ev,
						 	ResponseKeyword = "notifications.timeleft"
						 });

				await database.EventNotifications.AddRangeAsync(notcollection);
			}

			try
			{
				await database.SaveChangesAsync();
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "Agenda", $"Attempted to plan an event into the database, but failed: {e.Message}\n{e.StackTrace}"));
				return false;
			}
			return true;
		}

		public async Task<bool> Cancel(SocketGuild guild, string name)
		{
			// get the corresponding event from the database
			var ev = (from e in database.Events
						   where e.Guild.GuildID == guild.Id && e.Name == name
						   orderby e.Date
						   select e).FirstOrDefault();

			// return failure if there was no match
			if (ev == null) return false;

			// delete all the event data from the database
#error ev.Notifications is null, where it shouldn't be
			database.EventNotifications.RemoveRange(ev.Notifications);
			database.Events.Remove(ev);

			try
			{
				// try to save the changes
				await database.SaveChangesAsync();
			}
			catch(Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Warning, "Agenda", $"Attempted to remove event from database, but failed: {e.Message}\n{e.StackTrace}"));
				return false;
			}

			// return success
			return true;
		}

		public IEnumerable<EventTB> GetEvents(SocketGuild guild)
		{
			return from e in database.Events
				   where e.Guild.GuildID == guild.Id
				   orderby e.Date
				   select e;
		}
	}
}
