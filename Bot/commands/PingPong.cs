using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;

namespace Betty.commands
{
	public class PingPong : ModuleBase<SocketCommandContext>
	{
		public IServiceProvider services { get; set; }
		StateCollection statecollection;

		public PingPong(IServiceProvider services)
		{
			this.services = services;
			statecollection = services.GetService<StateCollection>();
		}

		[Command("ping"), Summary("Returns the ball like a pro")]
		public async Task Ping([Remainder]string input = null)
		{
			await Context.Channel.TriggerTypingAsync();
			string response = statecollection.GetLanguage(Context.Guild).GetString("command.ping");
			await Context.Channel.SendMessageAsync(response);
		}
	}
}
