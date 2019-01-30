using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Betty.commands
{
	public partial class Moderation : ModuleBase<SocketCommandContext>
	{
		Constants constants;

		public Moderation(IServiceProvider services)
		{
			constants = services.GetService<Constants>();
		}
	}
}
