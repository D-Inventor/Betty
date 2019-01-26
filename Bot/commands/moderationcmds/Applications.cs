using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

using Discord.Commands;
using Discord.WebSocket;
using Discord;

namespace Betty.commands
{
	public partial class Moderation
	{
		[Command("appstart"), Summary("Starts an application session by creating an invite url and an applications channel")]
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
			if(usertimezone == null)
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
			if(utcdeadline < DateTime.UtcNow)
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
			var invite = await settings.GetApplicationInvite(guild);
			await invite?.DeleteAsync();
		}

		[Command("appstop"), Summary("Stops an application session by destroying the invite url and the applications channel")]
		public async Task StopApplication([Remainder]string input = null)
		{
			await Context.Channel.TriggerTypingAsync();

			// cancel the event from the agenda
			agenda.Cancel(Context.Guild, "Application selection");
			await ExpireAppLink(Context.Guild);

			await settings.StopApplication(Context.Guild);
			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.appstop.success"));
		}
	}
}
