using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Betty.databases.guilds;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Betty.utilities
{
	public static class CommandMethods
	{
		public static void LogExecution(Logger logger, string command, SocketCommandContext Context)
		{
			logger.Log(new LogMessage(LogSeverity.Info, "Commands", $"Received '{command}' command from {Context.User.Username} in {Context.Guild.Name}:{Context.Channel.Name}"));
		}
	}
}
