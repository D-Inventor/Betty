using Betty.Services;
using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Betty.Utilities.DiscordUtilities
{
    public static class Extensions
    {
        public static void Log(this ILogger logger, LogMessage message)
        {
            switch (message.Severity)
            {
                case Discord.LogSeverity.Verbose:
                case Discord.LogSeverity.Debug:
                    logger.LogDebug(message.Source, message.Message);
                    break;
                case Discord.LogSeverity.Info:
                    logger.LogInfo(message.Source, message.Message);
                    break;
                case Discord.LogSeverity.Warning:
                    logger.LogWarning(message.Source, message.Message);
                    break;
                case Discord.LogSeverity.Error:
                case Discord.LogSeverity.Critical:
                    logger.LogError(message.Source, message.Message);
                    break;
            }
        }
    }
}
