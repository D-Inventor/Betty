using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Betty;

namespace Betty.commands
{
	public partial class Moderation
	{
		private async Task set_public(string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			settings.SetPublicChannel(Context.Guild, Context.Channel.Id);

			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.set.public"));
		}

		private async Task unset_public(string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			settings.SetPublicChannel(Context.Guild, null);

			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.unset.public"));
		}
	}
}
