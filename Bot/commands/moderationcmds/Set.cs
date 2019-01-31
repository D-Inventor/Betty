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
	[Group("set"), Summary("Sets a given setting to a given value")]
	public class Set : ModuleBase<SocketCommandContext>
	{
		StateCollection statecollection;
		Constants constants;
		Logger logger;

		public Set(IServiceProvider services)
		{
			this.statecollection = services.GetService<StateCollection>();
			this.constants = services.GetService<Constants>();
			this.logger = services.GetService<Logger>();
		}

		[Command("public"), Summary("Sets the channel of execution as the public channel")]
		public async Task set_public([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "set public", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			statecollection.SetPublicChannel(Context.Guild, Context.Channel.Id);
			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.set.public"));
		}

		[Command("notification"), Alias("notifications"), Summary("Sets the channel of execution as the notification channel")]
		public async Task set_notification([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "set notification", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			statecollection.SetNotificationChannel(Context.Guild, Context.Channel.Id);
			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.set.notification"));
		}

		[Command("timezones"), Alias("timezone"), Summary("Goes through all the timezones and creates/updates them accordingly")]
		public async Task set_timezones([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "set timezones", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.set.timezones.wait"));

			// find all the current roles in the guild
			IEnumerable<SocketRole> present = from role in Context.Guild.Roles
											  where DateTimeMethods.IsTimezone(role.Name)
											  select role;

			// add all the timezones that are not present already
			foreach (string t in DateTimeMethods.Timezones())
			{
				if (!present.Any(x => x.Name == t))
				{
					await Context.Guild.CreateRoleAsync(t, isHoisted: false, permissions: constants.RolePermissions);
				}
			}

			// update the roles of the timezones that áre present
			foreach(SocketRole sr in present)
			{
				await sr.ModifyAsync((x) =>
				{
					x.Permissions = constants.RolePermissions;
					x.Mentionable = true;
				});
			}

			// return success to the user
			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.set.timezones.done"));
		}
	}
}
