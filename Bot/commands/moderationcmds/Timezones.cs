using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;

namespace Betty.commands
{
	public partial class Moderation
	{
		public async Task set_timezones(string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.set.timezones.wait"));

			// find all the current roles in the guild
			IEnumerable<string> present = Context.Guild.Roles.Select(r => r.Name);

			// add all the timezones as roles if the timezone isn't already present
			foreach(string t in services.GetService<DateTimeUtils>().Timezones())
			{
				if (!present.Contains(t))
				{
					await Context.Guild.CreateRoleAsync(t, isHoisted: false, permissions:constants.RolePermissions);
				}
			}

			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.set.timezones.done"));
		}

		public async Task unset_timezones(string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.unset.timezones.wait"));

			// find all the current roles in the guild
			IEnumerable<KeyValuePair<string, ulong>> roles = Context.Guild.Roles.Select(r => new KeyValuePair<string, ulong>(r.Name, r.Id));

			// delete all the roles that are timezones
			foreach(var t in roles)
			{
				if (services.GetService<DateTimeUtils>().IsTimezone(t.Key))
				{
					await Context.Guild.GetRole(t.Value).DeleteAsync();
				}
			}

			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.unset.timezones.done"));
		}
	}
}
