using System;
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
        [RequireContext(ContextType.Guild)]
		[Command("cancel"), Summary("Removes an appointment from the agenda")]
		public async Task Cancel([Remainder]string input = null)
		{
			using(var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "cancel", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				var language = statecollection.GetLanguage(Context.Guild, database);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Member, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				// make sure that the input is not empty
				if (input == null)
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.cancel.empty"));
					return;
				}

				// try to remove event with given name from the agenda
				if (!agenda.Cancel(Context.Guild, database, input))
				{
					// return failure to the user
					await Context.Channel.SendMessageAsync(language.GetString("command.cancel.notplanned"));
					return;
				}

				// return success to the user
				await Context.Channel.SendMessageAsync(language.GetString("command.cancel.success", new SentenceContext()
																											.Add("title", input)));
			}
		}
	}
}
