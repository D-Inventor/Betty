using System.Threading.Tasks;
using System.Linq;
using Discord.Commands;
using Betty.utilities;

namespace Betty.commands
{
	public partial class Moderation
	{
		[Command("updateroles"), Summary("Makes sure that the roles are up to date")]
		public async Task UpdateRoles([Remainder]string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync("Updating the roles to the latest settings");
			foreach(var r in Context.Guild.Roles.Where(x => DateTimeMethods.IsTimezone(x.Name)))
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
