using Discord;
using Discord.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

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
