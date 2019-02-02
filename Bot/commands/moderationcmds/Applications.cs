using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Betty.utilities;
using Betty.databases.guilds;

namespace Betty.commands
{
	[Group("app"), Alias("application", "applications"), Summary("All commands, regarding application sessions")]
	public class Applications : ModuleBase<SocketCommandContext>
	{
		Agenda agenda;
		Logger logger;
		StateCollection statecollection;
		Notifier notifier;
		Constants constants;

		public Applications(IServiceProvider services)
		{
			agenda = services.GetService<Agenda>();
			logger = services.GetService<Logger>();
			statecollection = services.GetService<StateCollection>();
			notifier = services.GetService<Notifier>();
			constants = services.GetService<Constants>();
		}

		[Command("start"), Alias("begin"), Summary("Starts an application session by creating an invite url and an applications channel")]
		public async Task StartApplications([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "application start", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			GuildTB dbentry = statecollection.GetGuildEntry(Context.Guild);

			var language = statecollection.GetLanguage(Context.Guild, dbentry);

			// allow only 1 application per guild
			if (statecollection.GetApplicationEntry(Context.Guild) != null)
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
			TimeZoneInfo usertimezone = DateTimeMethods.UserToTimezone(Context.User);
			if (usertimezone == null)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.appstart.notimezone"));
				return;
			}

			// given deadline must be in proper format
			DateTime? localdeadline = DateTimeMethods.StringToDatetime(input);
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
			IInviteMetadata invite = await statecollection.StartApplication(Context.Guild, utcdeadline, dbentry);
			if (invite == null)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.appstart.error"));
				return;
			}

			// return success to the user
			await Context.Channel.SendMessageAsync(language.GetString("command.appstart.success", new SentenceContext()
																										.Add("url", invite.Url)));
		}

		[Command("stop"), Alias("end"), Summary("Stops an application session by destroying the invite url and the applications channel")]
		public async Task StopApplication([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "application stop", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			// make sure that applications are taking place
			ApplicationTB appentry = statecollection.GetApplicationEntry(Context.Guild);
			if (appentry == null)
			{
				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.appstop.noapp"));
				return;
			}

			// stop the applications
			await statecollection.StopApplication(Context.Guild, appentry);
			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.appstop.success"));
		}
	}
}
