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
	public partial class Moderation
	{
		[Group("set"), Summary("Sets a given setting to a given value")]
		public class Set : ModuleBase<SocketCommandContext>
		{
			StateCollection statecollection;
			Constants constants;
			public Set(IServiceProvider services)
			{
				this.statecollection = services.GetService<StateCollection>();
				this.constants = services.GetService<Constants>();
			}

			[Command("public"), Summary("Sets the channel of execution as the public channel")]
			public async Task set_public([Remainder]string input = null)
			{
				await Context.Channel.TriggerTypingAsync();
				statecollection.SetPublicChannel(Context.Guild, Context.Channel.Id);

				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.set.public"));
			}

			[Command("notification"), Alias("notifications"), Summary("Sets the channel of execution as the notification channel")]
			public async Task set_notification([Remainder]string input = null)
			{
				await Context.Channel.TriggerTypingAsync();
				statecollection.SetNotificationChannel(Context.Guild, Context.Channel.Id);

				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.set.notification"));
			}

			[Command("timezones"), Alias("timezone"), Summary("Creates all the required roles to perform time queries")]
			public async Task set_timezones([Remainder]string input = null)
			{
				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.set.timezones.wait"));

				// find all the current roles in the guild
				IEnumerable<string> present = Context.Guild.Roles.Select(r => r.Name);

				// add all the timezones as roles if the timezone isn't already present
				foreach (string t in DateTimeMethods.Timezones())
				{
					if (!present.Contains(t))
					{
						await Context.Guild.CreateRoleAsync(t, isHoisted: false, permissions: constants.RolePermissions);
					}
				}

				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild).GetString("command.set.timezones.done"));
			}
		}
	}
}
