using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.WebSocket;

using Betty.utilities;
using Betty.databases.guilds;

namespace Betty
{
	public class NotifierFactory
	{
		IServiceProvider services;

		public NotifierFactory(IServiceProvider services)
		{
			 this.services = services;
		}

		public Notifier Create(StateCollection statecollection)
		{
			return new Notifier(services, statecollection);
		}
	}

	public class Notifier
	{
		StateCollection statecollection;
		Logger logger;

		public Notifier(IServiceProvider services, StateCollection statecollection)
		{
			this.statecollection = statecollection;
			logger = services.GetService<Logger>();
		}

		public CancellationTokenSource CreateWaiterTask(SocketGuild guild, SocketTextChannel channel, bool doNotifications = true, IEnumerable<TimedMessage> messages = null, Action<GuildDB> action = null)
		{
			// create tokensource for cancellation
			var tokensource = new CancellationTokenSource();

			// create and run the task
			Task.Run(async () =>
			{
				var token = tokensource.Token;

				if(messages != null && messages.Any())
				{
					// wait for all notifications
					foreach (var m in messages.OrderBy(x => x.Date))
					{
						// if cancellation is requested, break the loop
						if (token.IsCancellationRequested) break;

						// don't do notifications that should've happened in the past
						if (m.Date < DateTime.UtcNow) continue;

						try
						{
							// wait for the date of given message
							await DateTimeMethods.WaitForDate(m.Date, token);
							if (doNotifications && !token.IsCancellationRequested)
							{
								using(var database = new GuildDB())
								{
									// send given message to discord
									var c = channel ?? statecollection.GetNotificationElsePublicChannel(guild, database);
									await c.SendMessageAsync(statecollection.GetLanguage(guild, database).GetString(m.Keyword, m.Context));
								}
							}
						}
						catch (TaskCanceledException) { }
						catch (Exception e)
						{
							logger.Log(new LogMessage(LogSeverity.Warning, "Notifier", $"Attempted to run notifier, but failed: {e.Message}\n{e.StackTrace}"));
						}
					}
				}
				else
				{
					logger.Log(new LogMessage(LogSeverity.Warning, "Notifier", $"Message container was empty. The action will be triggered immediately"));
				}

				// perform the action once all the messages have passed
				if (!token.IsCancellationRequested)
					using (var database = new GuildDB())
						action?.Invoke(database);

				logger.Log(new LogMessage(LogSeverity.Info, "Notifier", $"Finished waiter task for '{guild.Name}'"));
			});

			logger.Log(new LogMessage(LogSeverity.Info, "Notifier", $"Started a waiter task for '{guild.Name}'"));
			return tokensource;
		}
	}

	public struct TimedMessage
	{
		public string Keyword;
		public DateTime Date;
		public SentenceContext Context;
	}
}
