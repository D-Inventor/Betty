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

				// then add all the other notifications
				if (notifications != null) notcollection.AddRange
						(from n in notifications
						 select new EventNotificationTB
						 {
						 	DateTime = date - n,
						 	Event = ev,
						 	ResponseKeyword = "notifications.timeleft"
						 });

				// add these notifications to the event
				database.EventNotifications.AddRange(notcollection);
			}

			database.Events.Add(ev);

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
			try
			{
				// get the corresponding event from the database
				var ev = (from e in database.Events
							   where e.Guild.GuildId == guild.Id && e.Name == name
							   orderby e.Date
							   select e).FirstOrDefault();

				// return failure if there was no match
				if (ev == null) return false;

				// get all the notifications from the database
				var en = from e in database.EventNotifications
						 where e.Event.EventId == ev.EventId
						 select e;

				// delete all the event data from the database
				database.EventNotifications.RemoveRange(en);
				database.Events.Remove(ev);
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Error, "Agenda", $"Attempted to cancel event, but failed: {e.Message}\n{e.StackTrace}"));
			}

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
				   where e.Guild.GuildId == guild.Id
				   orderby e.Date
				   select e;
		}
	}
}
