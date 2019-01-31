using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;

using Betty.utilities;

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
			// log command execution
			CommandMethods.LogExecution(logger, "ping", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			try
			{
				string response = statecollection.GetLanguage(Context.Guild).GetString("command.ping");
				await Context.Channel.SendMessageAsync(response);
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Error, "Commands", $"Attempted to ping, but failed: {e.Message}\n{e.StackTrace}"));
			}
		}
	}
}
