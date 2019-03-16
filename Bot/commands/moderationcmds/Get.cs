using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Betty.databases.guilds;
using Betty.utilities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Betty.commands
{
	[Group("get"), Summary("Gets a setting for a specific guild")]
	public class Get : ModuleBase<SocketCommandContext>
	{
		StateCollection statecollection;
		Logger logger;
		Agenda agenda;
		Constants constants;

		public Get(IServiceProvider services)
		{
			this.statecollection = services.GetService<StateCollection>();
			this.logger = services.GetService<Logger>();
			this.agenda = services.GetService<Agenda>();
			this.constants = services.GetService<Constants>();
		}

		[Command("status"), Alias("state"), Summary("Returns everything that's known about a given guild")]
		public async Task Status([Remainder]string input = null)
		{
			using(var database = new GuildDB())
			{
				// log execution
				CommandMethods.LogExecution(logger, "status", Context);

				// indicate that the command is being worked on
				await Context.Channel.TriggerTypingAsync();

				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				StringConverter language = statecollection.GetLanguage(Context.Guild, database, gtb);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Admin, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				// get all the data from the database
				ApplicationTB apptb = statecollection.GetApplicationEntry(Context.Guild, database);
                SocketTextChannel pc = statecollection.GetPublicChannel(Context.Guild, database, gtb);
                SocketTextChannel nc = statecollection.GetNotificationChannel(Context.Guild, database, gtb);

                EmbedBuilder eb = new EmbedBuilder()
                    .WithColor(Color.Gold)
                    .WithTitle($":clipboard: Status information for '{gtb.Name}'");

				eb.AddField("Language", gtb.Language);
                eb.AddField("Public channel", (pc != null ? pc.Mention : "Undefined"));
                eb.AddField("Notification channel", (nc != null ? nc.Mention : "Undefined"));
                eb.AddField("Application", (apptb != null ? $"ends on: {apptb.Deadline.ToString("dd MMMM a\\t hh:mm tt UTC")}" : "No active application"));

				await Context.Channel.SendMessageAsync(statecollection.GetLanguage(Context.Guild, database, gtb).GetString("present"), embed: eb.Build());
			}
		}

        [Command("event"), Alias("events"), Summary("Command for displaying events from the database")]
        public async Task Events(EventSpecifier eventSpecifier, [Remainder]string rest = null)
        {
            using(var database = new GuildDB())
            {
                // log execution
                CommandMethods.LogExecution(logger, "event", Context);

                // indicate that the command is being worked on
                await Context.Channel.TriggerTypingAsync();

                GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
                StringConverter language = statecollection.GetLanguage(Context.Guild, database, gtb);

                // make sure that the user has the right permissions
                if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Member, database))
                {
                    await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
                    return;
                }

                var events = agenda.GetEvents(Context.Guild, database);

                // make sure that the agenda is not empty
                if(events.Count() == 0)
                {
                    await Context.Channel.SendMessageAsync(language.GetString("command.event.empty"));
                    return;
                }

                // create an embed
                EmbedBuilder eb = new EmbedBuilder()
                    .WithColor(Color.DarkRed);

                switch (eventSpecifier)
                {
                    case EventSpecifier.All:
                        eb.Title = $":calendar_spiral: All events for '{Context.Guild.Name}'";
                        foreach (EventTB e in events) eb.AddField(e.Name, e.Date.ToString(@"dd MMMM \a\t hh:mm tt UTC"));
                        break;
                    case EventSpecifier.First:
                        eb.Title = $":calendar_spiral: First event for '{Context.Guild.Name}'";
                        EventTB ev = events.First();
                        eb.AddField(ev.Name, ev.Date.ToString(@"dd MMMM \a\t hh:mm tt UTC"));
                        break;
                }

                Embed embed = eb.Build();
                await Context.Channel.SendMessageAsync(language.GetString("present"), embed: embed);
            }
        }

        [Command("event"), Alias("events"), Summary("Command for displaying events from the database")]
        public async Task Events()
        {
            await Events(EventSpecifier.All);
        }

		[Command("languages"), Alias("Language"), Summary("Gets all available languages.")]
		public async Task Languages()
		{
			using (var database = new GuildDB())
			{
				// log execution
				CommandMethods.LogExecution(logger, "get languages", Context);

				// indicate that the bot is working on the answer
				await Context.Channel.TriggerTypingAsync();

				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				var language = statecollection.GetLanguage(Context.Guild, database, gtb);

				// make sure that caller has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Admin, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				EmbedBuilder eb = new EmbedBuilder()
									.WithTitle(":speech_balloon: Languages")
									.WithColor(Color.DarkBlue);

				// get the current language
				eb.AddField("Current Language", gtb.Language);

				// find all available languages
				IEnumerable<string> languages = Directory.GetFiles(constants.PathToLanguages(), "*.lang").Select(x => Path.GetFileNameWithoutExtension(x)).OrderBy(x => x);
				eb.AddField("Available Languages", languages.Aggregate((x, y) => $"{x}\n{y}"));

				// send feedback to caller
				await Context.Channel.SendMessageAsync(language.GetString("present"), embed: eb.Build());
			}
		}

        public enum EventSpecifier
        {
            First,
            All
        }
	}
}
