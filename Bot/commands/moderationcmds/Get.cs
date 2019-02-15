using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Betty.databases.guilds;
using Betty.utilities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Betty.commands
{
	[Group("get"), Summary("Gets a setting for a specific guild")]
	public class Get : ModuleBase<SocketCommandContext>
	{
		StateCollection statecollection;
		Logger logger;
		Agenda agenda;

		public Get(IServiceProvider services)
		{
			this.statecollection = services.GetService<StateCollection>();
			this.logger = services.GetService<Logger>();
			this.agenda = services.GetService<Agenda>();
		}

		[Command("status"), Alias("state"), Summary("Returns everything that's known about a given guild")]
		public async Task Status([Remainder]string input = null)
		{
			using(var database = new GuildDB())
			{
				// log execution
				CommandMethods.LogExecution(logger, "status", Context);

				// indicate that the command is being worked on
				await Context.Channel.TriggerTypingAsync();

				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				StringConverter language = statecollection.GetLanguage(Context.Guild, database, gtb);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Admin, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				// get all the data from the database
				ApplicationTB apptb = statecollection.GetApplicationEntry(Context.Guild, database);
				IEnumerable<EventTB> evs = agenda.GetEvents(Context.Guild, database);

				// create a string with all status information
				StringBuilder result = new StringBuilder();
				result.AppendLine($"Status information for '{gtb.Name}':");

				// channel data
				SocketTextChannel pc = statecollection.GetPublicChannel(Context.Guild, database, gtb);
				result.AppendLine($"\tPublic channel: {(pc != null ? pc.Mention : "Undefined")}");
				SocketTextChannel nc = statecollection.GetNotificationChannel(Context.Guild, database, gtb);
				result.AppendLine($"\tNotification channel: {(nc != null ? nc.Mention : "Undefined")}");

				// application data
				result.AppendLine($"\tApplication deadline: {(apptb != null ? apptb.Deadline.ToString("dd MMMM a\\t hh:mm tt UTC") : "No active application")}");

				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild, database, gtb).GetString("present"));
				await Context.Channel.SendMessageAsync(result.ToString());
			}
		}
	}
}
