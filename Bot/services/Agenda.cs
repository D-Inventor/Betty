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
		#region Fields
		Logger logger;
		GuildDB database;
		StateCollection statecollection;
		Notifier notifier;
		Constants constants;

		ConcurrentDictionary<ulong, CancellationTokenSource> notifiercollection;
		#endregion

		public Agenda(IServiceProvider services)
		{
			logger = services.GetRequiredService<Logger>();
			database = services.GetRequiredService<GuildDB>();
			statecollection = services.GetRequiredService<StateCollection>();
			notifier = services.GetRequiredService<NotifierFactory>().Create(statecollection);
			constants = services.GetRequiredService<Constants>();

			notifiercollection = new ConcurrentDictionary<ulong, CancellationTokenSource>();
		}

		#region Public interface
		public void RestoreGuildEvents(SocketGuild guild)
		{
			// get all events and notifications for this guild
			var evs = GetEventsAndNotifications(guild);
			foreach(var ev in evs)
			{
				// check if this event has already passed
				if(ev.Event.Date < DateTime.UtcNow)
				{
					// remove from the database
					database.EventNotifications.RemoveRange(ev.Notifications);
					database.Events.Remove(ev.Event);
					continue;
				}

				SocketTextChannel channel = null;
				if(ev.Event.NotificationChannel != null)
				{
					try
					{
						// try to get the channel from the guild
						guild.GetTextChannel(ev.Event.NotificationChannel.Value);
					}
					catch(Exception e)
					{
						// log failure
						logger.Log(new LogMessage(LogSeverity.Warning, "Agenda", $"Attempted to get text channel from '{guild.Name}', but failed: {e.Message}\n{e.StackTrace}"));
					}
				}

				// create a waiter event
				var token = notifier.CreateWaiterTask(guild, channel, messages: TimedMessagesEvent(ev), action: async () =>
				{
					await Cancel(ev.Event);
				});
				
				// store cancellation token
				if (!notifiercollection.TryAdd(ev.Event.EventId, token))
				{
					// log failure
					logger.Log(new LogMessage(LogSeverity.Error, "Agenda", "Attempted to store cancellation token for event, but failed"));
				}
			}
		}

		public async Task<bool> Plan(SocketGuild guild, string name, DateTime date, SocketTextChannel channel = null, bool doNotifications = true, TimeSpan[] notifications = null)
		{
			// generate database data
			var ev = CreateEventEntries(guild, name, date, channel, doNotifications, notifications);

			database.Events.Add(ev.Event);
			try
			{
				// try to add event to the database
				await database.SaveChangesAsync();
			}
			catch (Exception e)
			{
				// log failure and report back
				logger.Log(new LogMessage(LogSeverity.Warning, "Agenda", $"Attempted to plan an event into the database, but failed: {e.Message}\n{e.StackTrace}"));
				return false;
			}

			// create notifiers for this event
			var token = notifier.CreateWaiterTask(guild, null, messages: DateTimeMethods.BuildMessageList(constants.EventNotifications, ev.Event.Date, ev.Event.Name), action: async () =>
			{
				await Cancel(ev.Event);
			});

			// store cancellation token
			if (!notifiercollection.TryAdd(ev.Event.EventId, token))
			{
				// log failure and report back
				logger.Log(new LogMessage(LogSeverity.Error, "Agenda", "Attempted to store cancellation token for event, but failed"));
				return false;
			}

			return true;
		}

		public async Task<bool> Cancel(SocketGuild guild, string name)
		{
			// get the corresponding event from the database
			var ev = (from e in database.Events
					  where e.Guild.GuildId == guild.Id && e.Name == name
					  orderby e.Date
					  select e).FirstOrDefault();

			// return failure if there was no match
			if (ev == null) return false;

			// get all the notifications for given event from the database
			var en = from e in database.EventNotifications
					 where e.Event.EventId == ev.EventId
					 select e;

			// delete all the event data from the database
			database.EventNotifications.RemoveRange(en);
			database.Events.Remove(ev);

			// cancel the notifications
			notifiercollection.Remove(ev.EventId, out CancellationTokenSource token);
			token.Cancel();

			try
			{
				// try to save the changes
				await database.SaveChangesAsync();
				return true;
			}
			catch (Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Warning, "Agenda", $"Attempted to remove event from database, but failed: {e.Message}\n{e.StackTrace}"));
				return false;
			}
		}

		public async Task<bool> Cancel(EventTB Event)
		{
			// get all the notifications from the database
			var en = from e in database.EventNotifications
					 where e.Event.EventId == Event.EventId
					 select e;

			database.EventNotifications.RemoveRange(en);
			database.Events.Remove(Event);

			// cancel the notifications
			notifiercollection.Remove(Event.EventId, out CancellationTokenSource token);
			token.Cancel();

			try
			{
				await database.SaveChangesAsync();
				return true;
			}
			catch (Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "Agenda", $"Attempted to cancel event '{Event.Name}', but failed: {e.Message}\n{e.StackTrace}"));
				return false;
			}
		}

		#region Getters
		public IEnumerable<EventTB> GetEvents(SocketGuild guild)
		{
			return from e in database.Events
				   where e.Guild.GuildId == guild.Id
				   orderby e.Date
				   select e;
		}

		public IEnumerable<EventAndNotifications> GetEventsAndNotifications(SocketGuild guild)
		{
			return from ev in database.Events
				   where ev.Guild.GuildId == guild.Id
				   join en in database.EventNotifications on ev equals en.Event into NotificationGroup
				   select new EventAndNotifications { Event = ev, Notifications = NotificationGroup };
		}
		#endregion
		#endregion

		#region Private methods
		private EventAndNotifications CreateEventEntries(SocketGuild guild, string name, DateTime date, SocketTextChannel channel = null, bool doNotifications = true, IEnumerable<TimeSpan> notifications = null)
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

			// return the event and the notifications
			return new EventAndNotifications
			{
				Event = ev,
				Notifications = doNotifications ? CreateNotificationEntries(ev, notifications) : null,
			};
		}

		private IEnumerable<EventNotificationTB> CreateNotificationEntries(EventTB ev, IEnumerable<TimeSpan> notifications)
		{
			// first return all the notifications in the list
			foreach(var ts in notifications)
			{
				yield return new EventNotificationTB
				{
					Date = ev.Date - ts,
					Event = ev,
					ResponseKeyword = "notifications.timeleft",
				};
			}

			// return a final notification on the deadline itself
			yield return new EventNotificationTB
			{
				Date = ev.Date,
				Event = ev,
				ResponseKeyword = "notifications.deadline",
			};
		}

		private IEnumerable<TimedMessage> TimedMessagesEvent(EventAndNotifications ev)
		{
			foreach(var en in ev.Notifications)
			{

				yield return new TimedMessage
				{
					Date = en.Date,
					Keyword = en.ResponseKeyword,
					Context = new SentenceContext()
						.Add("title", ev.Event.Name)
						.Add("time", DateTimeMethods.TimeSpanToString(ev.Event.Date - en.Date)),
				};
			}
		}
		#endregion
	}

	public struct EventAndNotifications
	{
		public EventTB Event { get; set; }
		public IEnumerable<EventNotificationTB> Notifications { get; set; }
	}
}
