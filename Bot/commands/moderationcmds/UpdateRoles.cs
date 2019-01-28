using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;

namespace Betty.commands
{
	public partial class Moderation
	{
		[Command("updateroles"), Summary("Makes sure that the roles are up to date")]
		public async Task UpdateRoles([Remainder]string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync("Updating the roles to the latest settings");
			foreach(var r in Context.Guild.Roles.Where(x => datetimeutils.IsTimezone(x.Name)))
			{
				await r.ModifyAsync(x =>
				{
					x.Permissions = constants.RolePermissions;
					x.Mentionable = true;
				});
			}
			await Context.Channel.SendMessageAsync("Updated all the roles");
		}
	}
}
