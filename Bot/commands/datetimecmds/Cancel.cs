using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Betty.utilities;

namespace Betty.commands
{
	public partial class DateConvert
	{
		[Command("cancel"), Summary("Removes an appointment from the agenda")]
		public async Task Cancel([Remainder]string input = null)
		{
			await Context.Channel.TriggerTypingAsync();

			var language = statecollection.GetLanguage(Context.Guild);

			if (input == null)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.cancel.empty"));
				return;
			}

			if (!services.GetService<Agenda>().Cancel(Context.Guild, input))
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.cancel.notplanned"));
				return;
			}

			await Context.Channel.SendMessageAsync(language.GetString("command.cancel.success", new SentenceContext()
																										.Add("title", input)));
		}
	}
}
