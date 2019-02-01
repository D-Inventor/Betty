﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.WebSocket;

using Betty.utilities;

namespace Betty
{
	public class Notifier
	{
		StateCollection statecollection;
		Logger logger;

		public Notifier(IServiceProvider services)
		{
			statecollection = services.GetService<StateCollection>();
			logger = services.GetService<Logger>();
		}

		public CancellationTokenSource CreateWaiterTask(SocketGuild guild, SocketTextChannel channel, bool doNotifications = true, IEnumerable<TimedMessage> messages = null, Action action = null)
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
						// don't do notifications that should've happened in the past
						if (m.Date < DateTime.UtcNow) continue;

						try
						{
							// wait for the date of given message
							await DateTimeMethods.WaitForDate(m.Date, token);
							if (doNotifications)
							{
								// send given message to discord
								var c = channel ?? statecollection.GetNotificationElsePublic(guild);
								await c.SendMessageAsync(statecollection.GetLanguage(guild).GetString(m.Keyword, m.Context));
							}
						}
						catch(Exception e)
						{
							logger.Log(new LogMessage(LogSeverity.Warning, "Notifier", $"Attempted to run notifier, but failed: {e.Message}"));
						}
					}
				}
				else
				{
					logger.Log(new LogMessage(LogSeverity.Warning, "Notifier", $"Message container was empty. The action will be triggered immediately"));
				}

				// perform the action once all the messages have passed
				if (!token.IsCancellationRequested)
					action?.Invoke();

				logger.Log(new LogMessage(LogSeverity.Info, "Notifier", $"Finished waiter task for {guild.Name}"));
			});

			logger.Log(new LogMessage(LogSeverity.Info, "Notifier", $"Started a waiter task for {guild.Name}"));
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
