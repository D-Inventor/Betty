using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Betty.commands
{
	public partial class DateConvert
	{
		[Command("event"), Summary("Gets the nearest event on the agenda")]
		public async Task Event([Remainder]string input = null)
		{
			await Context.Channel.TriggerTypingAsync();

			(var title, var date) = services.GetService<Agenda>().GetEvents(Context.Guild).FirstOrDefault();

			var language = settings.GetLanguage(Context.Guild);

			if (title == null)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.event.empty"));
				return;
			}

			await Context.Channel.SendMessageAsync(language.GetString("command.event.present", new SentenceContext()
																									.Add("title", title)
																									.Add("date", $"{date:dd MMMM} at {date:hh:mm tt} UTC")));
		}
	}
}
