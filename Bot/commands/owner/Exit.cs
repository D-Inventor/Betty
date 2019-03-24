using Betty.utilities;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Betty.commands
{
    public class Exit : ModuleBase<SocketCommandContext>
    {
        Bot bot;
        Logger logger;
        public Exit(IServiceProvider services)
        {
            bot = services.GetService<Bot>();
            logger = services.GetService<Logger>();
        }

        [RequireOwner]
        [Command("kill"), Alias("exit", "quit"), Summary("Shuts Betty down")]
        public async Task Kill()
        {
            // log command execution
            CommandMethods.LogExecution(logger, "kill", Context);

            bot.Stop();
        }

        [RequireOwner]
        [Command("restart"), Summary("Automatically restarts Betty")]
        public async Task Restart()
        {
            // log command execution
            CommandMethods.LogExecution(logger, "restart", Context);

            bot.Stop(true);
        }
    }
}
