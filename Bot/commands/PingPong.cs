using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;

using Betty.utilities;
using Betty.databases.guilds;
using Discord.WebSocket;

namespace Betty.commands
{
	public class PingPong : ModuleBase<SocketCommandContext>
	{
		StateCollection statecollection;
		Logger logger;

		public PingPong(IServiceProvider services)
		{
			statecollection = services.GetService<StateCollection>();
			logger = services.GetService<Logger>();
		}

		[Command("ping"), Summary("Returns the ball like a pro")]
		public async Task Ping([Remainder]string input = null)
		{
			using (var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "ping", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

                string response;
                if(Context.Guild != null)
                {
				    var language = statecollection.GetLanguage(Context.Guild, database);

				    // make sure that the user has the right permissions
				    if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Public, database))
				    {
					    await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					    return;
				    }

				    response = statecollection.GetLanguage(Context.Guild, database).GetString("command.ping");
                }
                else
                {
                    response = "pong";
                }
				await Context.Channel.SendMessageAsync(response);
			}
		}
	}
}
