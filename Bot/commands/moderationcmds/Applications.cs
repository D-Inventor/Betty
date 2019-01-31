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

namespace Betty.commands
{
	[Group("app"), Alias("application", "applications"), Summary("All commands, regarding application sessions")]
	public class Applications : ModuleBase<SocketCommandContext>
	{
		Agenda agenda;
		Logger logger;
		StateCollection statecollection;

		public Applications(IServiceProvider services)
		{
			agenda = services.GetService<Agenda>();
			logger = services.GetService<Logger>();
			statecollection = services.GetService<StateCollection>();
		}

		[Command("start"), Alias("begin"), Summary("Starts an application session by creating an invite url and an applications channel")]
		public async Task StartApplications([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "application start", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			var language = statecollection.GetLanguage(Context.Guild);

			// allow only 1 application per guild
			if (statecollection.GetApplicationActive(Context.Guild))
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
			IInviteMetadata invite = await statecollection.StartApplication(Context.Guild, utcdeadline);
			if (invite == null)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.appstart.error"));
				return;
			}

#warning TODO: add notification

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
			if (!statecollection.GetApplicationActive(Context.Guild))
			{
				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.appstop.noapp"));
				return;
			}

#warning TODO: cancel notification

			await ExpireAppLink(Context.Guild);

			await statecollection.StopApplication(Context.Guild);
			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.appstop.success"));
		}

		private async Task ExpireAppLink(SocketGuild guild)
		{
			try
			{
				var invite = await statecollection.GetApplicationInvite(guild);
				await invite?.DeleteAsync();
			}
			catch (Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "Commands", $"Attempted to delete invitation in '{guild.Name}', but failed: {e.Message}", e));
			}
		}
	}
}
