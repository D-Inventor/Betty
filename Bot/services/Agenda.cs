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
			var evs = GetEvents(guild);
			foreach(var ev in evs)
			{
				// find corresponding notifications
				var ens = from en in database.EventNotifications
						  where en.Event == ev
						  select en;

				// make sure that given event is still valid
				if (!CheckEventValidity(ev, ens)) continue;

				// get event channel
				logger.Log(new LogMessage(LogSeverity.Info, "Agenda", $"Getting text channel for '{ev.Name}' from '{guild.Name}'"));
				SocketTextChannel channel = GetChannel(guild, ev);

				// create a waiter event
				logger.Log(new LogMessage(LogSeverity.Info, "Agenda", $"Creating waiter task for '{ev.Name}' from '{guild.Name}'"));
				var token = notifier.CreateWaiterTask(guild, channel, messages: TimedMessagesFromEvent(new EventAndNotifications { Event = ev, Notifications = ens }), action: () =>
				{
					Cancel(ev);
				});
				
				// store cancellation token
				if (!notifiercollection.TryAdd(ev.EventId, token))
				{
					// log failure
					logger.Log(new LogMessage(LogSeverity.Error, "Agenda", "Attempted to store cancellation token for event, but failed"));
				}
			}
		}

		public bool Plan(SocketGuild guild, string name, DateTime date, SocketTextChannel channel = null, bool doNotifications = true, TimeSpan[] notifications = null)
		{
			// generate database data
			var ev = StoreEventInDatabase(guild, name, date, channel, doNotifications, notifications);

			// create notifier for this event
			var token = notifier.CreateWaiterTask(guild, null, messages: DateTimeMethods.BuildMessageList(constants.EventNotifications, ev.Event.Date, ev.Event.Name), action: () =>
			{
				Cancel(ev.Event);
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

		public bool Cancel(SocketGuild guild, string name)
		{
			// get the corresponding event from the database
			var ev = (from e in database.Events
					  where e.Guild.GuildId == guild.Id && e.Name == name
					  orderby e.Date
					  select e).FirstOrDefault();

			// return failure if there was no match
			if (ev == null) return false;

			// cancel the notifications
			CancelNotifier(ev);

			// remove from database
			return RemoveEventFromDatabase(ev);
		}

		public bool Cancel(EventTB Event)
		{
			// cancel the notifications
			CancelNotifier(Event);

			// remove from the database
			return RemoveEventFromDatabase(Event);
		}

		#region Getters
		public IEnumerable<EventTB> GetEvents(SocketGuild guild)
		{
			return from e in database.Events
				   where e.Guild.GuildId == guild.Id
				   orderby e.Date
				   select e;
		}
		#endregion
		#endregion

		#region Private methods
		private EventAndNotifications StoreEventInDatabase(SocketGuild guild, string name, DateTime date, SocketTextChannel channel = null, bool doNotifications = true, IEnumerable<TimeSpan> notifications = null)
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

			IEnumerable<EventNotificationTB> ens = null;
			if (doNotifications) ens = CreateNotificationEntries(ev, notifications);

			ev.Notifications = ens.ToArray();

			database.Events.Add(ev);
			try
			{
				// try to save addition to database
				database.SaveChanges();
			}
			catch(Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Error, "Agenda", $"Attempted to write event to database, but failed: {e.Message}\n{e.StackTrace}"));
			}

			// return the event and the notifications
			return new EventAndNotifications
			{
				Event = ev,
				Notifications = ens,
			};
		}

		private bool RemoveEventFromDatabase(EventTB ev)
		{
			// get all the notifications from the database
			var en = from e in database.EventNotifications
					 where e.Event.EventId == ev.EventId
					 select e;

			database.EventNotifications.RemoveRange(en);
			database.Events.Remove(ev);

			try
			{
				// try to apply removal to database
				database.SaveChanges();
				return true;
			}
			catch(Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Error, "Agenda", $"Attempted to remove event from database, but failed: {e.Message}\n{e.StackTrace}"));
				return false;
			}
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

		private IEnumerable<TimedMessage> TimedMessagesFromEvent(EventAndNotifications ev)
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

		private bool CheckEventValidity(EventTB ev, IEnumerable<EventNotificationTB> ens)
		{
			// check if this event has already passed
			if (ev.Date < DateTime.UtcNow)
			{
				// remove from the database
				database.EventNotifications.RemoveRange(ens);
				database.Events.Remove(ev);
				return false;
			}
			return true;
		}

		private SocketTextChannel GetChannel(SocketGuild guild, EventTB ev)
		{
			// return null if no channel has been set
			if (ev.NotificationChannel == null) return null;

			try
			{
				// try to get the channel from the guild
				return guild.GetTextChannel(ev.NotificationChannel.Value);
			}
			catch(Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Warning, "Agenda", $"Attempted to get channel from '{guild.Name}', but failed: {e.Message}\n{e.StackTrace}"));
				return null;
			}
		}

		private void CancelNotifier(EventTB ev)
		{
			notifiercollection.Remove(ev.EventId, out CancellationTokenSource token);
			token.Cancel();
		}
		#endregion
	}

	public struct EventAndNotifications
	{
		public EventTB Event { get; set; }
		public IEnumerable<EventNotificationTB> Notifications { get; set; }
	}
}
