using System;
using System.Collections.Generic;
using System.Text;

using Betty.Services;

namespace Betty.Utilities.DiscordUtilities
{
    public static class TypeTranslations
    {
        public static Discord.LogSeverity LogSeverity(LogSeverity logSeverity)
        {
            switch (logSeverity)
            {
                case Services.LogSeverity.Debug:
                    return Discord.LogSeverity.Debug;
                case Services.LogSeverity.Info:
                    return Discord.LogSeverity.Info;
                case Services.LogSeverity.Warning:
                    return Discord.LogSeverity.Warning;
                case Services.LogSeverity.Error:
                    return Discord.LogSeverity.Error;
                default:
                    throw new ArgumentException("Cannot convert given value to discord log severity", nameof(logSeverity));
            }
        }
    }
}
