using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Betty.utilities;

namespace Betty.commands
{
	[Group("unset"), Summary("unsets a given settings")]
	public class Unset : ModuleBase<SocketCommandContext>
	{
		StateCollection statecollection;
		Constants constants;
		Logger logger;

		public Unset(IServiceProvider services)
		{
			this.statecollection = services.GetService<StateCollection>();
			this.constants = services.GetService<Constants>();
			this.logger = services.GetService<Logger>();
		}

		[Command("public"), Summary("Sets the public channel to null")]
		public async Task unset_public([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "unset public", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			statecollection.SetPublicChannel(Context.Guild, null);
			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.unset.public"));
		}

		[Command("notification"), Alias("notifications"), Summary("Sets the notification channel to null")]
		public async Task unset_notification([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "unset notification", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			statecollection.SetNotificationChannel(Context.Guild, null);
			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.unset.notification"));
		}

		[Command("timezones"), Alias("timezone"), Summary("Removes all the roles for performing time queries")]
		public async Task unset_timezones([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "unset timezones", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.unset.timezones.wait"));

			// find all the current roles in the guild
			IEnumerable<KeyValuePair<string, ulong>> roles = Context.Guild.Roles.Select(r => new KeyValuePair<string, ulong>(r.Name, r.Id));

			// delete all the roles that are timezones
			foreach (var t in roles)
			{
				if (DateTimeMethods.IsTimezone(t.Key))
				{
					await Context.Guild.GetRole(t.Value).DeleteAsync();
				}
			}

			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.unset.timezones.done"));
		}
	}
}
