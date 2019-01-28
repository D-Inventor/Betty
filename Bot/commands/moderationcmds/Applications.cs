﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;
using Discord.WebSocket;
using Discord;

namespace Betty.commands
{
	public partial class Moderation
	{
		[Group("app"), Alias("application", "applications"), Summary("All commands, regarding application sessions")]
		public class Applications : ModuleBase<SocketCommandContext>
		{
			Settings settings;
			Agenda agenda;
			DateTimeUtils datetimeutils;
			Logger logger;

			public Applications(IServiceProvider services)
			{
				settings = services.GetService<Settings>();
				agenda = services.GetService<Agenda>();
				datetimeutils = services.GetService<DateTimeUtils>();
				logger = services.GetService<Logger>();
			}

			[Command("start"), Alias("begin"), Summary("Starts an application session by creating an invite url and an applications channel")]
			public async Task StartApplications([Remainder]string input = null)
			{
				await Context.Channel.TriggerTypingAsync();
				var language = settings.GetLanguage(Context.Guild);

				// allow only 1 application per guild
				if (settings.GetApplicationActive(Context.Guild))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.appstart.isactive"));
					return;
				}

				// command must have input
				if (input == null)
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.appstart.empty"));
					return;
				}

				// user must have a timezone
				TimeZoneInfo usertimezone = datetimeutils.UserToTimezone(Context.User);
				if (usertimezone == null)
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.appstart.notimezone"));
					return;
				}

				// given deadline must be in proper format
				DateTime? localdeadline = datetimeutils.StringToDatetime(input);
				if (!localdeadline.HasValue)
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.appstart.error"));
					return;
				}
				DateTime utcdeadline = TimeZoneInfo.ConvertTimeToUtc(localdeadline.Value, usertimezone);

				// deadline must be in the future
				if (utcdeadline < DateTime.UtcNow)
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.appstart.past"));
					return;
				}

				// creation must succeed
				IInviteMetadata invite = await settings.StartApplication(Context.Guild, utcdeadline);
				if (invite == null)
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.appstart.error"));
					return;
				}

				agenda.Plan(Context.Guild, "Application selection", utcdeadline, channelid: settings.GetApplicationChannel(Context.Guild), notifications: new TimeSpan[] { new TimeSpan(5, 0, 0), new TimeSpan(0, 30, 0) }, action: async () => await ExpireAppLink(Context.Guild), savetoharddrive: false);
				await Context.Channel.SendMessageAsync(language.GetString("command.appstart.success", new SentenceContext()
																											.Add("url", invite.Url)));
			}

			private async Task ExpireAppLink(SocketGuild guild)
			{
				try
				{
					var invite = await settings.GetApplicationInvite(guild);
					await invite?.DeleteAsync();
				}
				catch(Exception e)
				{
					logger.Log(new LogMessage(LogSeverity.Warning, "Commands", $"Attempted to delete invitation in '{guild.Name}', but failed: {e.Message}", e));
				}
			}

			[Command("stop"), Alias("end"), Summary("Stops an application session by destroying the invite url and the applications channel")]
			public async Task StopApplication([Remainder]string input = null)
			{
				await Context.Channel.TriggerTypingAsync();

				// make sure that applications are taking place
				if (!settings.GetApplicationActive(Context.Guild))
				{
					await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.appstop.noapp"));
					return;
				}

				// cancel the event from the agenda
				agenda.Cancel(Context.Guild, "Application selection");
				await ExpireAppLink(Context.Guild);

				await settings.StopApplication(Context.Guild);
				await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.appstop.success"));
			}
		}
	}
}
