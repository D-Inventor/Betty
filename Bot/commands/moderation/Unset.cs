using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Betty.utilities;
using Betty.databases.guilds;

namespace Betty.commands
{
	[Group("unset"), Summary("unsets a given settings")]
	public partial class Unset : ModuleBase<SocketCommandContext>
	{
		StateCollection statecollection;
		Constants constants;
		Logger logger;

		public Unset(IServiceProvider services)
		{
			this.statecollection = services.GetService<StateCollection>();
			this.constants = services.GetService<Constants>();
			this.logger = services.GetService<Logger>();
		}

		[Command("public"), Summary("Sets the public channel to null")]
		public async Task unset_public([Remainder]string input = null)
		{
			using (var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "unset public", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				// apply change and report to user
				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				var language = statecollection.GetLanguage(Context.Guild, database, gtb);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Owner, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				gtb.Public = null;
				statecollection.SetGuildEntry(gtb, database);
				await Context.Channel.SendMessageAsync(language.GetString("command.unset.public"));
			}
		}

		[Command("notification"), Alias("notifications"), Summary("Sets the notification channel to null")]
		public async Task unset_notification([Remainder]string input = null)
		{
			using (var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "unset notification", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				// apply change and report to user
				GuildTB gtb = statecollection.GetGuildEntry(Context.Guild, database);
				var language = statecollection.GetLanguage(Context.Guild, database, gtb);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Owner, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				gtb.Notification = null;
				statecollection.SetGuildEntry(gtb, database);
				await Context.Channel.SendMessageAsync(language.GetString("command.unset.notification"));
			}
		}

		[Command("timezones"), Alias("timezone"), Summary("Removes all the roles for performing time queries")]
		public async Task unset_timezones([Remainder]string input = null)
		{
			using (var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "unset timezones", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				var language = statecollection.GetLanguage(Context.Guild, database);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Owner, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				await Context.Channel.SendMessageAsync(language.GetString("command.unset.timezones.wait"));

				// find all the current roles in the guild
				IEnumerable<KeyValuePair<string, ulong>> roles = Context.Guild.Roles.Select(r => new KeyValuePair<string, ulong>(r.Name, r.Id));

				// delete all the roles that are timezones
				foreach (var t in roles)
				{
					if (DateTimeMethods.IsTimezone(t.Key))
					{
						await Context.Guild.GetRole(t.Value).DeleteAsync();
					}
				}

				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(language.GetString("command.unset.timezones.done"));
			}
		}
	}
}
