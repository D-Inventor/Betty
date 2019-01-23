using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Betty;

namespace Betty.commands
{
	public partial class Moderation
	{
		private async Task set_notification(string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			settings.SetNotificationChannel(Context.Guild, Context.Channel.Id);

			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.set.notification"));
		}

		private async Task unset_notification(string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			settings.SetNotificationChannel(Context.Guild, null);

			await Context.Channel.SendMessageAsync(settings.GetLanguage(Context.Guild).GetString("command.unset.public"));
		}
	}
}
