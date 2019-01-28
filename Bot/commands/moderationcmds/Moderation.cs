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
		DateTimeUtils datetimeutils;

		public Moderation(IServiceProvider services)
		{
			datetimeutils = services.GetService<DateTimeUtils>();
			constants = services.GetService<Constants>();
		}
	}
}
