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
        ManualResetEventSlim exitevent;
        Logger logger;
        public Exit(IServiceProvider services)
        {
            exitevent = services.GetService<ManualResetEventSlim>();
            logger = services.GetService<Logger>();
        }

        [RequireOwner]
        [Command("kill"), Alias("exit", "quit"), Summary("Shuts Betty down")]
        public async Task Kill()
        {
            // log command execution
            CommandMethods.LogExecution(logger, "kill", Context);

            // indicate that the bot is working on the command
            await Context.Channel.TriggerTypingAsync();

            exitevent.Set();
        }
    }
}
