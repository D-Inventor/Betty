using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Betty.commands
{
	public partial class Moderation
	{
		[Group("unset"), Summary("unsets a given settings")]
		public class Unset : ModuleBase<SocketCommandContext>
		{
			Settings settings;
			DateTimeUtils datetimeutils;
			Constants constants;
			public Unset(IServiceProvider services)
			{
				this.settings = services.GetService<Settings>();
				this.datetimeutils = services.GetService<DateTimeUtils>();
				this.constants = services.GetService<Constants>();
			}

			[Command("public"), Summary("Sets the public channel to null")]
			public async Task unset_public([Remainder]string input = null)
			{
				await Context.Channel.TriggerTypingAsync();
				settings.SetPublicChannel(Context.Guild, null);

				await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.unset.public"));
			}

			[Command("notification"), Alias("notifications"), Summary("Sets the notification channel to null")]
			public async Task unset_notification([Remainder]string input = null)
			{
				await Context.Channel.TriggerTypingAsync();
				settings.SetNotificationChannel(Context.Guild, null);

				await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.unset.notification"));
			}

			[Command("timezones"), Alias("timezone"), Summary("Removes all the roles for performing time queries")]
			public async Task unset_timezones([Remainder]string input = null)
			{
				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.unset.timezones.wait"));

				// find all the current roles in the guild
				IEnumerable<KeyValuePair<string, ulong>> roles = Context.Guild.Roles.Select(r => new KeyValuePair<string, ulong>(r.Name, r.Id));

				// delete all the roles that are timezones
				foreach (var t in roles)
				{
					if (datetimeutils.IsTimezone(t.Key))
					{
						await Context.Guild.GetRole(t.Value).DeleteAsync();
					}
				}

				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.unset.timezones.done"));
			}
		}
	}
}
