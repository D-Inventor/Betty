using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.DependencyInjection;

using Discord.WebSocket;
using Discord.Commands;
using Discord;
using Betty.utilities;

namespace Betty
{
	public class GuildState
	{
		public StringConverter Language { get; set; }
	}
}
