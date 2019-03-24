using Betty.databases.guilds;
using Betty.utilities;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Betty.commands
{
    public partial class Get
    {
        [RequireOwner]
        [Command("guilds"), Alias("guild"), Summary("Returns the list of guilds in which this bot is active")]
        public async Task Guilds()
        {
            using(var database = new GuildDB())
            {
                // log execution
                CommandMethods.LogExecution(logger, "get guilds", Context);

                // indicate that the command is being worked on
                await Context.Channel.TriggerTypingAsync();

                // get all the guilds from the database
                var dbresult = (from g in database.Guilds
                               orderby g.Name
                               select g.Name).ToArray();

                // display all guilds in an embed
                EmbedBuilder eb = new EmbedBuilder()
                    .WithColor(Color.Gold);

                eb.AddField("Guilds", dbresult.Aggregate((x, y) => $"{x}\n{y}"));
                
                await Context.Channel.SendMessageAsync("", embed: eb.Build());
            }
        }
    }
}
