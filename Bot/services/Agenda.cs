using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.WebSocket;
using Betty.utilities;

namespace Betty
{
	class Agenda
	{
		Constants constants;
		StateCollection statecollection;
		Logger logger;

		List<Event> eventcollection;

		public Agenda(IServiceProvider services)
		{
			constants = services.GetRequiredService<Constants>();
			statecollection = services.GetRequiredService<StateCollection>();
			logger = services.GetRequiredService<Logger>();

			eventcollection = new List<Event>();
		}

		public void Plan(SocketGuild guild, string title, DateTime date, Action action = null, bool donotifications = true, TimeSpan[] notifications = null, SocketTextChannel channel = null, bool savetoharddrive = true)
		{
			Event e = new Event(guild, title, date, notifications ?? constants.EventNotifications, statecollection, logger, donotifications, channel);
			lock (eventcollection)
			{
				eventcollection.Add(e);
			}

			Task.Run(async () =>
			{
				await e.AwaitDeadline(action);

				lock (eventcollection)
				{
					eventcollection.Remove(e);
				}
			});
		}

		public bool Cancel(SocketGuild guild, string title)
		{
			Event e = eventcollection.FirstOrDefault(x => x.Guild == guild && x.Title == title);
			if (e != null)
			{
				e.Cancel();
				return true;
			}
			return false;
		}

		public IEnumerable<ValueTuple<string, DateTime>> GetEvents(SocketGuild guild)
		{
			return eventcollection.Where(x => x.Guild.Id == guild.Id).OrderBy(x => x.Date).Select(x => new ValueTuple<string, DateTime>(x.Title, x.Date));
		}

		private class Event
		{
			SocketGuild guild;
			string title;
			DateTime date;
			bool donotifications;
			SocketTextChannel channelid;
			TimeSpan[] notifications;
			CancellationTokenSource tokensource;

			StateCollection statecollection;
			Logger logger;


			public Event(SocketGuild guild, string title, DateTime date, TimeSpan[] notifications, StateCollection statecollection, Logger logger, bool donotifications = true,  SocketTextChannel channelid = null)
			{
				this.guild = guild;
				this.title = title;
				this.date = date;
				this.donotifications = donotifications;
				this.channelid = channelid;
				this.notifications = notifications ?? (new TimeSpan[] { new TimeSpan(2, 0, 0), new TimeSpan(0, 30, 0) });
				Array.Sort(this.notifications);
				Array.Reverse(this.notifications);

				this.statecollection = statecollection;
				this.logger = logger;
				tokensource = new CancellationTokenSource();
			}

			public void Cancel()
			{
				tokensource.Cancel();
			}

			public async Task AwaitDeadline(Action action = null)
			{
				var token = tokensource.Token;

				// send on each notification date a notification
				if (donotifications)
				{
					foreach (var n in notifications)
					{
						if (date - n > DateTime.UtcNow)
						{
							try
							{
								await DateTimeMethods.WaitForDate(date - n, token);
								if(!token.IsCancellationRequested)
									await Notify(statecollection.GetLanguage(guild).GetString("notifications.timeleft", new SentenceContext()
																											.Add("time", DateTimeMethods.TimeSpanToString(n))
																											.Add("title", title)));
							}
							catch (Exception e) { logger.Log(new LogMessage(LogSeverity.Warning, "Agenda", e.Message, e)); }
						}
					}
				}

				// and on the deadline
				try
				{
					await DateTimeMethods.WaitForDate(date, token);
					if (!token.IsCancellationRequested)
					{
						if (donotifications)
						{
							await Notify(statecollection.GetLanguage(guild).GetString("notifications.deadline", new SentenceContext()
																											.Add("title", title)));
						}
						action?.Invoke();
					}
				}
				catch (Exception e) { logger.Log(new LogMessage(LogSeverity.Warning, "Agenda", e.Message, e)); }
			}

			private async Task Notify(string message)
			{
				// send notification to desired channel
				SocketTextChannel channel = channelid ?? statecollection.GetNotificationElsePublic(guild);
				await channel?.TriggerTypingAsync();
				await channel?.SendMessageAsync(message);
			}

			public SocketGuild Guild { get { return guild; } }
			public string Title { get { return title; } }
			public DateTime Date { get { return date; } }
		}
	}
}
