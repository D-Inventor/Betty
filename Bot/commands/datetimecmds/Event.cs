using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Betty.utilities;
using Betty.databases.guilds;

namespace Betty.commands
{
	public partial class DateConvert
	{
		[Command("event"), Summary("Gets the nearest event on the agenda")]
		public async Task Event([Remainder]string input = null)
		{
			using(var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "event", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				// get the first event on the agenda if any
				EventTB e = agenda.GetEvents(Context.Guild, database).FirstOrDefault();

				var language = statecollection.GetLanguage(Context.Guild, database);

				// make sure that there is indeed an event
				if (e == null)
				{
					// return failure to the user
					await Context.Channel.SendMessageAsync(language.GetString("command.event.empty"));
					return;
				}

				// return success to the user
				await Context.Channel.SendMessageAsync(language.GetString("command.event.present", new SentenceContext()
																										.Add("title", e.Name)
																										.Add("date", $"{e.Date:dd MMMM} at {e.Date:hh:mm tt} UTC")));
			}
		}
	}
}
