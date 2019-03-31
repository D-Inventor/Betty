using Betty.databases.guilds;
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
        StateCollection statecollection;

        public Exit(IServiceProvider services)
        {
            bot = services.GetService<Bot>();
            logger = services.GetService<Logger>();
            statecollection = services.GetService<StateCollection>();
        }

        [RequireOwner]
        [Command("kill"), Alias("exit", "quit"), Summary("Shuts Betty down")]
        public async Task Kill()
        {
            using(var database = new GuildDB())
            {
                // log command execution
                CommandMethods.LogExecution(logger, "kill", Context);

                if(Context.Guild != null)
                {
                    StringConverter language = statecollection.GetLanguage(Context.Guild, database);
                    await Context.Channel.SendMessageAsync(language.GetString("command.exit"));
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Shutting down now...");
                }

                bot.Stop();
            }
        }

        [RequireOwner]
        [Command("restart"), Summary("Automatically restarts Betty")]
        public async Task Restart()
        {
            using (var database = new GuildDB())
            {
                // log command execution
                CommandMethods.LogExecution(logger, "restart", Context);

                if (Context.Guild != null)
                {
                    StringConverter language = statecollection.GetLanguage(Context.Guild, database);
                    await Context.Channel.SendMessageAsync(language.GetString("command.restart"));
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Restarting now...");
                }

                bot.Stop(true);
            }
        }
    }
}
